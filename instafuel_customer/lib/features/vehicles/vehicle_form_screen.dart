// Add or edit a vehicle, including its documents (registration PDF, fitness
// PDF, number plate photo). File picking/upload is NOT wired yet: tapping
// "Attach" just fills a placeholder file name so the flow can be reviewed.

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme.dart';
import '../../core/widgets/attachment_tile.dart';
import '../../core/widgets/responsive_center.dart';
import '../../core/widgets/section_card.dart';
import '../../core/widgets/status_chip.dart';
import '../../models/fuel.dart';
import '../../models/vehicle.dart';
import 'vehicles_controller.dart';

class VehicleFormScreen extends ConsumerStatefulWidget {
  const VehicleFormScreen({super.key, this.vehicleId});

  final String? vehicleId; // null = new vehicle

  @override
  ConsumerState<VehicleFormScreen> createState() => _VehicleFormScreenState();
}

class _VehicleFormScreenState extends ConsumerState<VehicleFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nickname = TextEditingController();
  final _plate = TextEditingController();
  VehicleType _type = VehicleType.car;
  FuelType _fuel = FuelType.octane;
  final _docs = <DocumentKind, String?>{for (final k in DocumentKind.values) k: null};
  Vehicle? _existing;
  bool _saving = false;

  bool get _isEdit => widget.vehicleId != null;

  @override
  void initState() {
    super.initState();
    if (_isEdit) {
      _existing = ref.read(vehiclesProvider).value?.where((v) => v.id == widget.vehicleId).firstOrNull;
      final v = _existing;
      if (v != null) {
        _nickname.text = v.nickname;
        _plate.text = v.registrationNumber;
        _type = v.type;
        _fuel = v.fuelType;
        for (final k in DocumentKind.values) {
          _docs[k] = v.document(k).fileName;
        }
      }
    }
  }

  @override
  void dispose() {
    _nickname.dispose();
    _plate.dispose();
    super.dispose();
  }

  // Placeholder until file_picker / image_picker + storage are added.
  void _fakeAttach(DocumentKind kind) {
    setState(() => _docs[kind] = kind.isPhoto ? 'number_plate.jpg' : '${kind.name}.pdf');
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Demo: real file picking will be added later.')),
    );
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);

    final vehicle = Vehicle(
      id: _existing?.id ?? '',
      nickname: _nickname.text.trim(),
      type: _type,
      registrationNumber: _plate.text.trim(),
      fuelType: _fuel,
      verificationStatus: _existing?.verificationStatus ?? VerificationStatus.pending,
      documents: [for (final e in _docs.entries) VehicleDocument(kind: e.key, fileName: e.value)],
    );

    try {
      final saved = await ref.read(vehiclesProvider.notifier).save(vehicle);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(saved.isVerified
            ? '${saved.nickname} saved and verified'
            : saved.documentsComplete
                ? '${saved.nickname} saved. We\'ll verify the documents shortly.'
                : '${saved.nickname} saved. Add all documents to get it verified.'),
      ));
      context.pop();
    } catch (e) {
      if (mounted) {
        setState(() => _saving = false);
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }

  Future<void> _delete() async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Remove vehicle?'),
        content: const Text('You will no longer be able to order fuel for it.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Keep')),
          FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Remove')),
        ],
      ),
    );
    if (ok != true || !mounted) return;
    try {
      await ref.read(vehiclesProvider.notifier).delete(widget.vehicleId!);
      if (mounted) context.pop();
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    const gap = SizedBox(height: AppTheme.gap16);

    return Scaffold(
      appBar: AppBar(
        title: Text(_isEdit ? 'Edit vehicle' : 'Add vehicle'),
        actions: [
          if (_isEdit)
            IconButton(tooltip: 'Remove vehicle', icon: const Icon(Icons.delete_outline), onPressed: _delete),
        ],
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(AppTheme.gap16),
          children: [
            ResponsiveCenter(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (_existing != null) ...[
                    Align(alignment: Alignment.centerLeft, child: StatusChip.verification(_existing!.verificationStatus)),
                    if (_existing!.rejectionReason != null) ...[
                      const SizedBox(height: AppTheme.gap8),
                      Text(
                        '${_existing!.rejectionReason} Fix the documents and save to send it for review again.',
                        style: TextStyle(color: Theme.of(context).colorScheme.error),
                      ),
                    ],
                    gap,
                  ],
                  SectionCard(
                    title: 'Details',
                    child: Column(
                      children: [
                        TextFormField(
                          controller: _nickname,
                          textCapitalization: TextCapitalization.sentences,
                          decoration: const InputDecoration(labelText: 'Nickname', hintText: 'e.g. Family car'),
                          validator: (v) => (v == null || v.trim().isEmpty) ? 'Give the vehicle a name' : null,
                        ),
                        gap,
                        TextFormField(
                          controller: _plate,
                          textCapitalization: TextCapitalization.characters,
                          inputFormatters: [_UpperCaseFormatter()],
                          decoration: const InputDecoration(
                            labelText: 'Registration number',
                            hintText: 'DHAKA METRO-GA 12-3456',
                            helperText: 'Exactly as written on the number plate',
                          ),
                          validator: (v) => (v == null || v.trim().length < 5) ? 'Enter the full registration number' : null,
                        ),
                        gap,
                        DropdownButtonFormField<VehicleType>(
                          initialValue: _type,
                          decoration: const InputDecoration(labelText: 'Vehicle type'),
                          items: [
                            for (final t in VehicleType.values)
                              DropdownMenuItem(
                                value: t,
                                child: Row(children: [Icon(t.icon, size: 20), const SizedBox(width: 12), Text(t.label)]),
                              ),
                          ],
                          onChanged: (t) => setState(() => _type = t ?? _type),
                        ),
                        gap,
                        Align(
                          alignment: Alignment.centerLeft,
                          child: Text('Usual fuel', style: theme.textTheme.labelLarge),
                        ),
                        const SizedBox(height: AppTheme.gap8),
                        SizedBox(
                          width: double.infinity,
                          child: SegmentedButton<FuelType>(
                            showSelectedIcon: false,
                            segments: [for (final f in FuelType.values) ButtonSegment(value: f, label: Text(f.label))],
                            selected: {_fuel},
                            onSelectionChanged: (s) => setState(() => _fuel = s.first),
                          ),
                        ),
                      ],
                    ),
                  ),
                  gap,
                  SectionCard(
                    title: 'Documents',
                    trailing: Text(
                      '${_docs.values.where((f) => f != null).length}/${_docs.length}',
                      style: theme.textTheme.labelLarge,
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Needed to verify the vehicle. The number plate photo is what the driver '
                          'compares against at delivery.',
                          style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                        ),
                        const SizedBox(height: AppTheme.gap8),
                        for (final k in DocumentKind.values)
                          AttachmentTile(
                            document: VehicleDocument(kind: k, fileName: _docs[k]),
                            onAttach: () => _fakeAttach(k),
                            onRemove: () => setState(() => _docs[k] = null),
                          ),
                      ],
                    ),
                  ),
                  const SizedBox(height: AppTheme.gap24),
                  FilledButton(
                    onPressed: _saving ? null : _save,
                    child: _saving
                        ? const SizedBox.square(dimension: 22, child: CircularProgressIndicator(strokeWidth: 2.5))
                        : Text(_isEdit ? 'Save changes' : 'Add vehicle'),
                  ),
                  const SizedBox(height: AppTheme.gap8),
                  Text(
                    'Demo: vehicles with all 3 documents are verified instantly. '
                    'In production, our team reviews them first.',
                    textAlign: TextAlign.center,
                    style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _UpperCaseFormatter extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) =>
      newValue.copyWith(text: newValue.text.toUpperCase());
}
