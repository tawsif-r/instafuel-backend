// 4-step order flow adhering to SOP-OPS-001:
// 1. Vehicle/Equipment Selection
// 2. Fuel Type & Quantity (with minimum order validation & quick litre chips)
// 3. Delivery Address, 2-Hour Slot (enforcing 24h lead time) & Site Parking Notes
// 4. Invoicing Review (Fuel Charge + Distance Delivery Fee = Total Invoice)

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
  static const _minOrderLitres = 20.0; // SOP §5.4 Minimum order volume

  int _step = 0;
  final _lines = <String, _LineInput>{}; // vehicleId -> input
  final _address = TextEditingController();
  final _notes = TextEditingController();
  final _siteContact = TextEditingController();
  late DateTime _scheduledAt = _defaultTime();
  String _selectedSlotWindow = '10:00 - 12:00';
  String? _corporateId;
  Future<OrderQuote>? _quote;
  bool _placing = false;
  String? _stepError;

  // Next available day complying with SOP §5.2 (at least 24h ahead)
  static DateTime _defaultTime() {
    final t = DateTime.now().add(orderLeadTime + const Duration(hours: 2));
    return DateTime(t.year, t.month, t.day, 10, 0);
  }

  bool get _timeValid => !_scheduledAt.isBefore(DateTime.now().add(orderLeadTime));

  @override
  void dispose() {
    for (final l in _lines.values) {
      l.litres.dispose();
    }
    _address.dispose();
    _notes.dispose();
    _siteContact.dispose();
    super.dispose();
  }

  OrderDraft _draft() => OrderDraft(
        lines: [
          for (final e in _lines.entries)
            OrderLineDraft(vehicleId: e.key, fuelType: e.value.fuelType, litres: e.value.litresValue ?? 0),
        ],
        address: _address.text.trim(),
        scheduledAt: _scheduledAt,
        notes: _buildNotes(),
        corporateId: _corporateId,
      );

  String? _buildNotes() {
    final parts = <String>[];
    if (_siteContact.text.trim().isNotEmpty) {
      parts.add('Contact: ${_siteContact.text.trim()}');
    }
    if (_notes.text.trim().isNotEmpty) {
      parts.add(_notes.text.trim());
    }
    return parts.isEmpty ? null : parts.join(' · ');
  }

  String? _validateStep() {
    switch (_step) {
      case 0:
        return _lines.isEmpty ? 'Select at least one vehicle or generator to refuel.' : null;
      case 1:
        final bad = _lines.values.any((l) => (l.litresValue ?? 0) <= 0 || l.litresValue! > _maxLitres);
        if (bad) return 'Enter a valid quantity between 1 and ${_maxLitres.toInt()} litres.';
        final total = _lines.values.fold<double>(0, (sum, l) => sum + (l.litresValue ?? 0));
        if (total < _minOrderLitres) {
          return 'Minimum order requirement is ${_minOrderLitres.toInt()} litres (SOP §5.4). Current: ${total.toInt()}L.';
        }
        return null;
      case 2:
        if (_address.text.trim().isEmpty) return 'Enter the exact delivery location / address.';
        if (!_timeValid) return 'Order lead-time requirement: Delivery must be at least 24 hours ahead (SOP §5.2).';
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

  Future<void> _pickDate() async {
    final min = DateTime.now().add(orderLeadTime);
    final date = await showDatePicker(
      context: context,
      initialDate: _scheduledAt,
      firstDate: DateTime(min.year, min.month, min.day),
      lastDate: DateTime.now().add(const Duration(days: 30)),
    );
    if (date == null || !mounted) return;
    setState(() {
      _scheduledAt = DateTime(date.year, date.month, date.day, _scheduledAt.hour, _scheduledAt.minute);
      _stepError = null;
    });
  }

  void _selectSlot(String slot, int hour) {
    setState(() {
      _selectedSlotWindow = slot;
      _scheduledAt = DateTime(_scheduledAt.year, _scheduledAt.month, _scheduledAt.day, hour, 0);
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
      appBar: AppBar(
        title: const Text('Schedule Fuel Delivery'),
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(42),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
            color: const Color(0xFF059669).withValues(alpha: 0.1),
            child: const Row(
              children: [
                Icon(Icons.shield_outlined, size: 16, color: Color(0xFF059669)),
                SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'SOP-OPS-001 Compliant · 24h Advance Lead Time Required',
                    style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.bold, color: Color(0xFF059669)),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
      body: AsyncValueView(
        value: vehicles,
        data: (list) {
          if (list.isEmpty) {
            return EmptyState(
              icon: Icons.directions_car_outlined,
              title: 'No vehicles or equipment found',
              message: 'Before placing an order, add your private vehicle, commercial fleet, or backup generator.',
              action: FilledButton.icon(
                onPressed: () => context.push('/vehicle/new'),
                icon: const Icon(Icons.add),
                label: const Text('Add Vehicle / Generator'),
              ),
            );
          }

          return ResponsiveCenter(
            maxWidth: 680,
            child: Stepper(
              currentStep: _step,
              elevation: 0,
              type: StepperType.horizontal,
              onStepTapped: (s) {
                if (s < _step) setState(() => _step = s);
              },
              controlsBuilder: (_, _) => _controls(),
              steps: [
                Step(
                  title: const Text('Vehicles'),
                  isActive: _step >= 0,
                  state: _step > 0 ? StepState.complete : StepState.indexed,
                  content: _vehiclesStep(list),
                ),
                Step(
                  title: const Text('Fuel'),
                  isActive: _step >= 1,
                  state: _step > 1 ? StepState.complete : StepState.indexed,
                  content: _fuelStep(list),
                ),
                Step(
                  title: const Text('Delivery'),
                  isActive: _step >= 2,
                  state: _step > 2 ? StepState.complete : StepState.indexed,
                  content: _deliveryStep(),
                ),
                Step(
                  title: const Text('Invoice'),
                  isActive: _step >= 3,
                  state: StepState.indexed,
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
      padding: const EdgeInsets.only(top: AppTheme.gap24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (_stepError != null) ...[
            Container(
              padding: const EdgeInsets.all(AppTheme.gap12),
              decoration: BoxDecoration(
                color: const Color(0xFFFEF2F2),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFFFCA5A5)),
              ),
              child: Row(
                children: [
                  const Icon(Icons.error_outline_rounded, color: Color(0xFFDC2626), size: 18),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      _stepError!,
                      style: const TextStyle(color: Color(0xFFB91C1C), fontSize: 13, fontWeight: FontWeight.bold),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppTheme.gap12),
          ],
          Row(
            children: [
              if (_step > 0) ...[
                OutlinedButton(
                  onPressed: _placing ? null : _back,
                  style: OutlinedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  ),
                  child: const Text('Back'),
                ),
                const SizedBox(width: AppTheme.gap12),
              ],
              Expanded(
                child: _step < 3
                    ? FilledButton(
                        onPressed: _next,
                        style: FilledButton.styleFrom(
                          backgroundColor: const Color(0xFF059669),
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(vertical: 14),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        ),
                        child: const Text('Continue', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
                      )
                    : FilledButton.icon(
                        onPressed: _placing ? null : _placeOrder,
                        style: FilledButton.styleFrom(
                          backgroundColor: const Color(0xFF059669),
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(vertical: 14),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        ),
                        icon: _placing
                            ? const SizedBox.square(dimension: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                            : const Icon(Icons.lock_rounded, size: 18),
                        label: const Text('Confirm Order & Pay', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
                      ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  // ---------- Step 1: Vehicle Selection ----------
  Widget _vehiclesStep(List<Vehicle> vehicles) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Text(
          'Select vehicles or generators to receive fuel:',
          style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
        ),
        const SizedBox(height: 8),
        for (final v in vehicles)
          Container(
            margin: const EdgeInsets.only(bottom: 10),
            decoration: BoxDecoration(
              color: _lines.containsKey(v.id) ? const Color(0xFF059669).withValues(alpha: 0.08) : Colors.transparent,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(
                color: _lines.containsKey(v.id) ? const Color(0xFF059669) : Colors.grey.withValues(alpha: 0.25),
                width: _lines.containsKey(v.id) ? 1.5 : 1,
              ),
            ),
            child: CheckboxListTile(
              value: _lines.containsKey(v.id),
              contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
              secondary: Icon(v.type.icon, color: const Color(0xFF059669)),
              title: Text(v.nickname, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
              subtitle: Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Wrap(
                  spacing: 8,
                  runSpacing: 4,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    LicensePlate(v.registrationNumber, compact: true),
                    Text(v.fuelType.label, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Colors.grey)),
                    if (!v.isVerified) StatusChip.verification(v.verificationStatus),
                  ],
                ),
              ),
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
          ),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton.icon(
            onPressed: () => context.push('/vehicle/new'),
            icon: const Icon(Icons.add_rounded, color: Color(0xFF059669)),
            label: const Text('Register Another Vehicle or Generator', style: TextStyle(fontWeight: FontWeight.bold, color: Color(0xFF059669))),
          ),
        ),
      ],
    );
  }

  // ---------- Step 2: Fuel & Quantity ----------
  Widget _fuelStep(List<Vehicle> vehicles) {
    final prices = ref.watch(fuelProductsProvider).value ?? [];
    double? priceOf(FuelType t) => prices.where((p) => p.type == t).firstOrNull?.pricePerLitre;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: const Color(0xFFECFDF5),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: const Color(0xFFA7F3D0)),
          ),
          child: const Row(
            children: [
              Icon(Icons.info_outline_rounded, color: Color(0xFF047857), size: 18),
              SizedBox(width: 8),
              Expanded(
                child: Text(
                  'Minimum delivery order is 20 Litres per SOP §5.4 commercial standards.',
                  style: TextStyle(fontSize: 12, color: Color(0xFF065F46), fontWeight: FontWeight.w600),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: AppTheme.gap16),
        for (final entry in _lines.entries) ...[
          Container(
            padding: const EdgeInsets.all(AppTheme.gap16),
            decoration: BoxDecoration(
              color: Theme.of(context).cardTheme.color,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: Colors.grey.withValues(alpha: 0.25)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Builder(builder: (context) {
                  final v = vehicles.firstWhere((v) => v.id == entry.key);
                  return Row(
                    children: [
                      Icon(v.type.icon, color: const Color(0xFF059669)),
                      const SizedBox(width: AppTheme.gap8),
                      Expanded(
                        child: Text(v.nickname, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
                      ),
                      LicensePlate(v.registrationNumber, compact: true),
                    ],
                  );
                }),
                const SizedBox(height: AppTheme.gap12),
                const Text('Fuel Type', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
                const SizedBox(height: 6),
                SegmentedButton<FuelType>(
                  showSelectedIcon: false,
                  segments: [for (final t in FuelType.values) ButtonSegment(value: t, label: Text(t.label))],
                  selected: {entry.value.fuelType},
                  onSelectionChanged: (s) => setState(() => entry.value.fuelType = s.first),
                ),
                const SizedBox(height: AppTheme.gap16),
                const Text('Volume (Litres)', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
                const SizedBox(height: 6),
                Row(
                  children: [
                    for (final preset in [20, 35, 50, 75]) ...[
                      Padding(
                        padding: const EdgeInsets.only(right: 6),
                        child: ActionChip(
                          avatar: const Icon(Icons.water_drop_rounded, size: 14, color: Color(0xFF059669)),
                          label: Text('${preset}L', style: const TextStyle(fontSize: 11.5, fontWeight: FontWeight.bold)),
                          onPressed: () {
                            setState(() {
                              entry.value.litres.text = preset.toString();
                              _stepError = null;
                            });
                          },
                        ),
                      ),
                    ],
                  ],
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: entry.value.litres,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'^\d*\.?\d{0,2}'))],
                  onChanged: (_) => setState(() => _stepError = null),
                  decoration: InputDecoration(
                    labelText: 'Custom Litres',
                    suffixText: 'Litres',
                    helperText: priceOf(entry.value.fuelType) == null
                        ? null
                        : 'Govt. rate: ${formatMoney(priceOf(entry.value.fuelType)!)}/L · Est. fuel cost: ${formatMoney((entry.value.litresValue ?? 0) * priceOf(entry.value.fuelType)!)}',
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: AppTheme.gap16),
        ],
      ],
    );
  }

  // ---------- Step 3: Delivery Location & Slot ----------
  Widget _deliveryStep() {
    final previous = {...?ref.watch(ordersProvider).value?.map((o) => o.address)}.take(3);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Lead-time notice
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: const Color(0xFFFFFBEB),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: const Color(0xFFFDE68A)),
          ),
          child: const Row(
            children: [
              Icon(Icons.schedule_rounded, color: Color(0xFFD97706), size: 20),
              SizedBox(width: 8),
              Expanded(
                child: Text(
                  'SOP §5.2 Mandatory 24h Lead Time: Delivery slots are locked 24 hours prior for fleet route optimization.',
                  style: TextStyle(color: Color(0xFF92400E), fontSize: 12, fontWeight: FontWeight.bold),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: AppTheme.gap16),

        TextField(
          controller: _address,
          minLines: 1,
          maxLines: 3,
          textCapitalization: TextCapitalization.sentences,
          onChanged: (_) => setState(() => _stepError = null),
          decoration: const InputDecoration(
            labelText: 'Delivery Address & Location Pin',
            prefixIcon: Icon(Icons.location_on_rounded, color: Color(0xFF059669)),
            helperText: 'e.g. House 12, Road 5, Dhanmondi, Dhaka',
          ),
        ),
        if (previous.isNotEmpty) ...[
          const SizedBox(height: AppTheme.gap8),
          Wrap(
            spacing: AppTheme.gap8,
            children: [
              for (final a in previous)
                ActionChip(
                  avatar: const Icon(Icons.history_rounded, size: 16),
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

        // Date selection
        Card(
          child: ListTile(
            leading: const Icon(Icons.calendar_month_rounded, color: Color(0xFF059669)),
            title: Text(formatDate(_scheduledAt), style: const TextStyle(fontWeight: FontWeight.bold)),
            subtitle: const Text('Delivery Date (Select Date)'),
            trailing: const Icon(Icons.edit_calendar_rounded),
            onTap: _pickDate,
          ),
        ),
        const SizedBox(height: AppTheme.gap12),

        // 2-Hour Slot Selector (SOP §5.2)
        const Text('Select Delivery Window (2-Hour Slot):', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            _SlotChip(
              title: '08:00 - 10:00 (Morning)',
              hour: 8,
              isSelected: _selectedSlotWindow == '08:00 - 10:00',
              onTap: () => _selectSlot('08:00 - 10:00', 8),
            ),
            _SlotChip(
              title: '10:00 - 12:00 (Noon)',
              hour: 10,
              isSelected: _selectedSlotWindow == '10:00 - 12:00',
              onTap: () => _selectSlot('10:00 - 12:00', 10),
            ),
            _SlotChip(
              title: '14:00 - 16:00 (Afternoon)',
              hour: 14,
              isSelected: _selectedSlotWindow == '14:00 - 16:00',
              onTap: () => _selectSlot('14:00 - 16:00', 14),
            ),
            _SlotChip(
              title: '18:00 - 20:00 (Evening)',
              hour: 18,
              isSelected: _selectedSlotWindow == '18:00 - 20:00',
              onTap: () => _selectSlot('18:00 - 20:00', 18),
            ),
          ],
        ),
        const SizedBox(height: AppTheme.gap16),

        TextField(
          controller: _siteContact,
          decoration: const InputDecoration(
            labelText: 'On-Site Contact Person & Phone (SOP §4.1)',
            prefixIcon: Icon(Icons.contact_phone_outlined),
            hintText: 'e.g. Rahim (Security / Driver) - 017XXXXXXXX',
          ),
        ),
        const SizedBox(height: AppTheme.gap12),
        TextField(
          controller: _notes,
          decoration: const InputDecoration(
            labelText: 'Site Access Instructions (Gate, basement height, etc.)',
            prefixIcon: Icon(Icons.notes_rounded),
          ),
        ),
      ],
    );
  }

  // ---------- Step 4: Invoice & Review ----------
  Widget _reviewStep(List<Vehicle> vehicles) {
    final companies = (ref.watch(affiliationsProvider).value ?? []).where((a) => a.isActive).toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Container(
          padding: const EdgeInsets.all(AppTheme.gap16),
          decoration: BoxDecoration(
            color: Theme.of(context).cardTheme.color,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: Colors.grey.withValues(alpha: 0.25)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('ORDER SPECIFICATION (SOP §5.3)', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 13)),
              const Divider(height: 20),
              for (final e in _lines.entries) ...[
                Builder(builder: (context) {
                  final v = vehicles.firstWhere((veh) => veh.id == e.key);
                  return Row(
                    children: [
                      Icon(v.type.icon, size: 20, color: const Color(0xFF059669)),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text('${v.nickname} (${v.registrationNumber})', style: const TextStyle(fontWeight: FontWeight.bold)),
                      ),
                      Text('${e.value.fuelType.label} · ${formatLitres(e.value.litresValue ?? 0)}', style: const TextStyle(fontWeight: FontWeight.bold)),
                    ],
                  );
                }),
                const SizedBox(height: 8),
              ],
              const Divider(height: 20),
              Row(
                children: [
                  const Icon(Icons.schedule_rounded, size: 18, color: Colors.grey),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      '${formatDate(_scheduledAt)} · Window: $_selectedSlotWindow',
                      style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Row(
                children: [
                  const Icon(Icons.location_on_rounded, size: 18, color: Colors.grey),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(_address.text, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600)),
                  ),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: AppTheme.gap16),

        if (companies.isNotEmpty) ...[
          const Text('BILLING ACCOUNT (SOP §7.1)', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
          const SizedBox(height: 8),
          Wrap(
            spacing: AppTheme.gap8,
            children: [
              ChoiceChip(
                label: const Text('Direct Payment (Prepaid)'),
                selected: _corporateId == null,
                onSelected: (_) => setState(() => _corporateId = null),
              ),
              for (final CorporateAffiliation c in companies)
                ChoiceChip(
                  avatar: const Icon(Icons.business_center_rounded, size: 16),
                  label: Text('${c.companyName} (Credit)'),
                  selected: _corporateId == c.id,
                  onSelected: (_) => setState(() => _corporateId = c.id),
                ),
            ],
          ),
          const SizedBox(height: AppTheme.gap16),
        ],

        // Invoicing breakdown
        FutureBuilder<OrderQuote>(
          future: _quote,
          builder: (context, snap) {
            if (!snap.hasData) return const Center(child: Padding(padding: EdgeInsets.all(16), child: CircularProgressIndicator()));
            final q = snap.data!;
            return Container(
              padding: const EdgeInsets.all(AppTheme.gap16),
              decoration: BoxDecoration(
                color: const Color(0xFF0F172A),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Column(
                children: [
                  _priceRowDark('Fuel Charge (Litres × Unit Rate)', formatMoney(q.subtotal)),
                  const SizedBox(height: 6),
                  _priceRowDark('Zone-based Delivery Charge (Zone A)', formatMoney(q.deliveryFee)),
                  const Divider(color: Color(0xFF334155), height: 20),
                  _priceRowDark('Total Payable Invoice', formatMoney(q.total), isTotal: true),
                ],
              ),
            );
          },
        ),
        const SizedBox(height: AppTheme.gap12),
        const Text(
          '✓ Flow meter calibrated according to National Metrology standards.\n✓ Number plate and security seal verified before fueling.',
          style: TextStyle(fontSize: 11, color: Colors.grey, height: 1.4),
        ),
      ],
    );
  }

  Widget _priceRowDark(String label, String value, {bool isTotal = false}) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: TextStyle(
            color: isTotal ? Colors.white : Colors.white70,
            fontWeight: isTotal ? FontWeight.w900 : FontWeight.w500,
            fontSize: isTotal ? 16 : 13,
          ),
        ),
        Text(
          value,
          style: TextStyle(
            color: isTotal ? const Color(0xFF34D399) : Colors.white,
            fontWeight: isTotal ? FontWeight.w900 : FontWeight.w700,
            fontSize: isTotal ? 18 : 13,
          ),
        ),
      ],
    );
  }
}

class _SlotChip extends StatelessWidget {
  const _SlotChip({
    required this.title,
    required this.hour,
    required this.isSelected,
    required this.onTap,
  });

  final String title;
  final int hour;
  final bool isSelected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return ChoiceChip(
      label: Text(title, style: TextStyle(fontSize: 12, fontWeight: isSelected ? FontWeight.bold : FontWeight.w500)),
      selected: isSelected,
      onSelected: (_) => onTap(),
      selectedColor: const Color(0xFF059669),
      labelStyle: TextStyle(color: isSelected ? Colors.white : null),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      showCheckmark: false,
    );
  }
}
