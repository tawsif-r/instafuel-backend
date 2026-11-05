// 4-step order flow: vehicles -> fuel & quantity -> delivery -> review.
// One order can cover several vehicles; each becomes its own order line.

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../core/widgets/async_value_view.dart';
import '../../core/widgets/empty_state.dart';
import '../../core/widgets/license_plate.dart';
import '../../core/widgets/responsive_center.dart';
import '../../core/widgets/status_chip.dart';
import '../../data/providers.dart';
import '../../models/corporate.dart';
import '../../models/fuel.dart';
import '../../models/order.dart';
import '../../models/vehicle.dart';
import '../corporate/corporate_controller.dart';
import '../vehicles/vehicles_controller.dart';
import 'orders_controller.dart';

// Per-vehicle choices on step 2.
class _LineInput {
  _LineInput(this.fuelType) : litres = TextEditingController(text: '20');

  FuelType fuelType;
  final TextEditingController litres;

  double? get litresValue => double.tryParse(litres.text);
}

class NewOrderScreen extends ConsumerStatefulWidget {
  const NewOrderScreen({super.key});

  @override
  ConsumerState<NewOrderScreen> createState() => _NewOrderScreenState();
}

class _NewOrderScreenState extends ConsumerState<NewOrderScreen> {
  static const _maxLitres = 5000.0;

  int _step = 0;
  final _lines = <String, _LineInput>{}; // vehicleId -> input, in selection order
  final _address = TextEditingController();
  final _notes = TextEditingController();
  late DateTime _scheduledAt = _defaultTime();
  String? _corporateId; // null = I pay myself
  Future<OrderQuote>? _quote;
  bool _placing = false;
  String? _stepError;

  // Next full hour after the 24h minimum.
  static DateTime _defaultTime() {
    final t = DateTime.now().add(orderLeadTime + const Duration(hours: 1));
    return DateTime(t.year, t.month, t.day, t.hour);
  }

  bool get _timeValid => !_scheduledAt.isBefore(DateTime.now().add(orderLeadTime));

  @override
  void dispose() {
    for (final l in _lines.values) {
      l.litres.dispose();
    }
    _address.dispose();
    _notes.dispose();
    super.dispose();
  }

  OrderDraft _draft() => OrderDraft(
        lines: [
          for (final e in _lines.entries)
            OrderLineDraft(vehicleId: e.key, fuelType: e.value.fuelType, litres: e.value.litresValue ?? 0),
        ],
        address: _address.text.trim(),
        scheduledAt: _scheduledAt,
        notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(),
        corporateId: _corporateId,
      );

  // Returns an error message for the current step, or null if it's complete.
  String? _validateStep() {
    switch (_step) {
      case 0:
        return _lines.isEmpty ? 'Select at least one vehicle.' : null;
      case 1:
        final bad = _lines.values.any((l) => (l.litresValue ?? 0) <= 0 || l.litresValue! > _maxLitres);
        return bad ? 'Enter a quantity between 1 and ${_maxLitres.toInt()} litres for each vehicle.' : null;
      case 2:
        if (_address.text.trim().isEmpty) return 'Enter a delivery address.';
        if (!_timeValid) return 'Delivery must be at least 24 hours from now.';
        return null;
    }
    return null;
  }

  void _next() {
    final error = _validateStep();
    setState(() => _stepError = error);
    if (error != null) return;
    if (_step == 2) _quote = ref.read(orderRepositoryProvider).quote(_draft());
    if (_step < 3) setState(() => _step++);
  }

  void _back() => setState(() {
        _stepError = null;
        if (_step > 0) _step--;
      });

  Future<void> _pickTime() async {
    final min = DateTime.now().add(orderLeadTime);
    final date = await showDatePicker(
      context: context,
      initialDate: _scheduledAt,
      firstDate: DateTime(min.year, min.month, min.day),
      lastDate: DateTime.now().add(const Duration(days: 30)),
    );
    if (date == null || !mounted) return;
    final time = await showTimePicker(context: context, initialTime: TimeOfDay.fromDateTime(_scheduledAt));
    if (time == null) return;
    setState(() {
      _scheduledAt = DateTime(date.year, date.month, date.day, time.hour, time.minute);
      _stepError = null;
    });
  }

  Future<void> _placeOrder() async {
    setState(() => _placing = true);
    try {
      final order = await ref.read(ordersProvider.notifier).place(_draft());
      if (mounted) context.pushReplacement('/order/${order.id}/pay');
    } catch (e) {
      if (mounted) {
        setState(() => _placing = false);
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final vehicles = ref.watch(vehiclesProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('New order')),
      body: AsyncValueView(
        value: vehicles,
        onRetry: () => ref.invalidate(vehiclesProvider),
        data: (list) {
          if (list.isEmpty) {
            return EmptyState(
              icon: Icons.directions_car_outlined,
              title: 'Add a vehicle first',
              message: 'Fuel is only delivered to registered and verified vehicles.',
              action: FilledButton.icon(
                onPressed: () => context.push('/vehicle/new'),
                icon: const Icon(Icons.add),
                label: const Text('Add vehicle'),
              ),
            );
          }
          return ResponsiveCenter(
            child: Stepper(
              currentStep: _step,
              onStepTapped: (i) => i < _step ? setState(() => _step = i) : null, // only go back by tapping
              controlsBuilder: (context, details) => _controls(),
              steps: [
                Step(
                  title: const Text('Vehicles'),
                  subtitle: _step > 0 ? Text('${_lines.length} selected') : null,
                  isActive: _step >= 0,
                  state: _step > 0 ? StepState.complete : StepState.indexed,
                  content: _vehiclesStep(list),
                ),
                Step(
                  title: const Text('Fuel & quantity'),
                  isActive: _step >= 1,
                  state: _step > 1 ? StepState.complete : StepState.indexed,
                  content: _fuelStep(list),
                ),
                Step(
                  title: const Text('Delivery'),
                  subtitle: _step > 2 ? Text(formatDateTime(_scheduledAt)) : null,
                  isActive: _step >= 2,
                  state: _step > 2 ? StepState.complete : StepState.indexed,
                  content: _deliveryStep(),
                ),
                Step(
                  title: const Text('Review'),
                  isActive: _step >= 3,
                  content: _reviewStep(list),
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _controls() {
    return Padding(
      padding: const EdgeInsets.only(top: AppTheme.gap16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (_stepError != null) ...[
            Text(_stepError!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
            const SizedBox(height: AppTheme.gap12),
          ],
          Row(
            children: [
              if (_step > 0) ...[
                OutlinedButton(onPressed: _placing ? null : _back, child: const Text('Back')),
                const SizedBox(width: AppTheme.gap12),
              ],
              Expanded(
                child: _step < 3
                    ? FilledButton(onPressed: _next, child: const Text('Continue'))
                    : FilledButton.icon(
                        onPressed: _placing ? null : _placeOrder,
                        icon: _placing
                            ? const SizedBox.square(dimension: 18, child: CircularProgressIndicator(strokeWidth: 2))
                            : const Icon(Icons.lock_outline),
                        label: const Text('Place order & pay'),
                      ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  // ---------- Step 1 ----------
  Widget _vehiclesStep(List<Vehicle> vehicles) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (final v in vehicles)
          CheckboxListTile(
            value: _lines.containsKey(v.id),
            contentPadding: EdgeInsets.zero,
            secondary: Icon(v.type.icon),
            title: Text(v.nickname),
            subtitle: Padding(
              padding: const EdgeInsets.only(top: AppTheme.gap4),
              child: Wrap(
                spacing: AppTheme.gap8,
                runSpacing: AppTheme.gap4,
                crossAxisAlignment: WrapCrossAlignment.center,
                children: [
                  LicensePlate(v.registrationNumber, compact: true),
                  if (!v.isVerified) StatusChip.verification(v.verificationStatus),
                ],
              ),
            ),
            // Only verified vehicles can receive fuel.
            onChanged: v.isVerified
                ? (checked) => setState(() {
                      _stepError = null;
                      if (checked == true) {
                        _lines[v.id] = _LineInput(v.fuelType);
                      } else {
                        _lines.remove(v.id)?.litres.dispose();
                      }
                    })
                : null,
          ),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton.icon(
            onPressed: () => context.push('/vehicle/new'),
            icon: const Icon(Icons.add),
            label: const Text('Register another vehicle'),
          ),
        ),
      ],
    );
  }

  // ---------- Step 2 ----------
  Widget _fuelStep(List<Vehicle> vehicles) {
    final prices = ref.watch(fuelProductsProvider).value ?? [];
    double? priceOf(FuelType t) => prices.where((p) => p.type == t).firstOrNull?.pricePerLitre;

    return Column(
      children: [
        for (final entry in _lines.entries) ...[
          Card(
            child: Padding(
              padding: const EdgeInsets.all(AppTheme.gap12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Builder(builder: (context) {
                    final v = vehicles.firstWhere((v) => v.id == entry.key);
                    return Row(
                      children: [
                        Icon(v.type.icon),
                        const SizedBox(width: AppTheme.gap8),
                        Expanded(child: Text(v.nickname, style: Theme.of(context).textTheme.titleSmall)),
                        LicensePlate(v.registrationNumber, compact: true),
                      ],
                    );
                  }),
                  const SizedBox(height: AppTheme.gap12),
                  SegmentedButton<FuelType>(
                    showSelectedIcon: false,
                    segments: [for (final t in FuelType.values) ButtonSegment(value: t, label: Text(t.label))],
                    selected: {entry.value.fuelType},
                    onSelectionChanged: (s) => setState(() => entry.value.fuelType = s.first),
                  ),
                  const SizedBox(height: AppTheme.gap12),
                  TextField(
                    controller: entry.value.litres,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'^\d*\.?\d{0,2}'))],
                    onChanged: (_) => setState(() => _stepError = null),
                    decoration: InputDecoration(
                      labelText: 'Quantity',
                      suffixText: 'litres',
                      helperText: priceOf(entry.value.fuelType) == null
                          ? null
                          : '${formatMoney(priceOf(entry.value.fuelType)!)} per litre · '
                              '≈ ${formatMoney((entry.value.litresValue ?? 0) * priceOf(entry.value.fuelType)!)}',
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: AppTheme.gap12),
        ],
      ],
    );
  }

  // ---------- Step 3 ----------
  Widget _deliveryStep() {
    // Suggest addresses from previous orders.
    final previous = {...?ref.watch(ordersProvider).value?.map((o) => o.address)}.take(3);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        TextField(
          controller: _address,
          minLines: 1,
          maxLines: 3,
          textCapitalization: TextCapitalization.sentences,
          onChanged: (_) => setState(() => _stepError = null),
          decoration: const InputDecoration(
            labelText: 'Delivery address',
            prefixIcon: Icon(Icons.location_on_outlined),
            helperText: 'Where the vehicles will be parked',
          ),
        ),
        if (previous.isNotEmpty) ...[
          const SizedBox(height: AppTheme.gap8),
          Wrap(
            spacing: AppTheme.gap8,
            children: [
              for (final a in previous)
                ActionChip(
                  avatar: const Icon(Icons.history, size: 18),
                  label: Text(a, overflow: TextOverflow.ellipsis),
                  onPressed: () => setState(() {
                    _address.text = a;
                    _stepError = null;
                  }),
                ),
            ],
          ),
        ],
        const SizedBox(height: AppTheme.gap16),
        Card(
          child: ListTile(
            leading: const Icon(Icons.event_outlined),
            title: Text(formatDateTime(_scheduledAt)),
            subtitle: Text(
              _timeValid ? 'Delivery time' : 'Must be at least 24 hours from now',
              style: _timeValid ? null : TextStyle(color: Theme.of(context).colorScheme.error),
            ),
            trailing: const Icon(Icons.edit_outlined),
            onTap: _pickTime,
          ),
        ),
        const SizedBox(height: AppTheme.gap16),
        TextField(
          controller: _notes,
          minLines: 1,
          maxLines: 3,
          decoration: const InputDecoration(
            labelText: 'Notes for the driver (optional)',
            prefixIcon: Icon(Icons.sticky_note_2_outlined),
          ),
        ),
      ],
    );
  }

  // ---------- Step 4 ----------
  Widget _reviewStep(List<Vehicle> vehicles) {
    final theme = Theme.of(context);
    final companies = (ref.watch(affiliationsProvider).value ?? []).where((a) => a.isActive).toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (final e in _lines.entries)
          ListTile(
            contentPadding: EdgeInsets.zero,
            leading: Icon(vehicles.firstWhere((v) => v.id == e.key).type.icon),
            title: Text(vehicles.firstWhere((v) => v.id == e.key).nickname),
            subtitle: Text('${e.value.fuelType.label} · ${formatLitres(e.value.litresValue ?? 0)}'),
          ),
        ListTile(
          contentPadding: EdgeInsets.zero,
          leading: const Icon(Icons.location_on_outlined),
          title: Text(_address.text),
          subtitle: Text(formatDateTime(_scheduledAt)),
        ),
        if (companies.isNotEmpty) ...[
          const SizedBox(height: AppTheme.gap8),
          Text('Bill to', style: theme.textTheme.titleSmall),
          const SizedBox(height: AppTheme.gap8),
          Wrap(
            spacing: AppTheme.gap8,
            runSpacing: AppTheme.gap8,
            children: [
              ChoiceChip(
                label: const Text('Myself'),
                selected: _corporateId == null,
                onSelected: (_) => setState(() => _corporateId = null),
              ),
              for (final CorporateAffiliation c in companies)
                ChoiceChip(
                  avatar: const Icon(Icons.business_outlined, size: 18),
                  label: Text(c.companyName),
                  selected: _corporateId == c.id,
                  onSelected: (_) => setState(() => _corporateId = c.id),
                ),
            ],
          ),
        ],
        const Divider(height: AppTheme.gap32),
        FutureBuilder<OrderQuote>(
          future: _quote,
          builder: (context, snap) {
            if (!snap.hasData) return const Center(child: CircularProgressIndicator());
            final q = snap.data!;
            return Column(
              children: [
                _priceRow('Fuel', formatMoney(q.subtotal)),
                _priceRow('Delivery fee', formatMoney(q.deliveryFee)),
                const SizedBox(height: AppTheme.gap8),
                _priceRow('Total', formatMoney(q.total), bold: true),
              ],
            );
          },
        ),
        const SizedBox(height: AppTheme.gap8),
        Text(
          'The driver will match each vehicle’s number plate before fuelling. '
          'Final amount is based on the quantity actually delivered.',
          style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
        ),
      ],
    );
  }

  Widget _priceRow(String label, String value, {bool bold = false}) {
    final style = bold ? Theme.of(context).textTheme.titleMedium : Theme.of(context).textTheme.bodyMedium;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(children: [Expanded(child: Text(label, style: style)), Text(value, style: style)]),
    );
  }
}
