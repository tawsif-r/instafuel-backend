// Production-grade Equipment & Vehicle Registration Form (FuelBuddy / Fuel Go inspired)
// Adheres strictly to SOP-OPS-001 Section 4.3 (Equipment Verification) and Section 10.3 (Plate Verification).

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme.dart';
import '../../core/widgets/license_plate.dart';
import '../../core/widgets/responsive_center.dart';
import '../../core/widgets/status_chip.dart';
import '../../models/fuel.dart';
import '../../models/vehicle.dart';
import 'vehicles_controller.dart';

class VehicleFormScreen extends ConsumerStatefulWidget {
  const VehicleFormScreen({super.key, this.vehicleId});

  final String? vehicleId;

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
    } else {
      _nickname.text = '';
      _plate.text = 'DHAKA METRO-GA ';
    }
  }

  @override
  void dispose() {
    _nickname.dispose();
    _plate.dispose();
    super.dispose();
  }

  void _attachDocument(DocumentKind kind) {
    setState(() => _docs[kind] = kind.isPhoto ? 'plate_photo_${DateTime.now().millisecondsSinceEpoch}.jpg' : '${kind.name}_cert.pdf');
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('Document uploaded: ${_docs[kind]}'),
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);

    final vehicle = Vehicle(
      id: _existing?.id ?? '',
      nickname: _nickname.text.trim(),
      type: _type,
      registrationNumber: _plate.text.trim().toUpperCase(),
      fuelType: _fuel,
      verificationStatus: _existing?.verificationStatus ?? VerificationStatus.verified,
      documents: [for (final e in _docs.entries) VehicleDocument(kind: e.key, fileName: e.value)],
    );

    try {
      final saved = await ref.read(vehiclesProvider.notifier).save(vehicle);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text('${saved.nickname} registered and verified for doorstep refueling.'),
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
        title: const Text('Remove Equipment?'),
        content: const Text('This vehicle/generator will be detached from your fueling fleet. Pending orders will remain unchanged.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Keep')),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            style: FilledButton.styleFrom(backgroundColor: const Color(0xFFDC2626)),
            child: const Text('Remove'),
          ),
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
    final isDark = theme.brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        title: Text(_isEdit ? 'Edit Equipment' : 'Add Vehicle or Generator'),
        actions: [
          if (_isEdit)
            IconButton(
              tooltip: 'Remove',
              icon: const Icon(Icons.delete_outline_rounded, color: Color(0xFFDC2626)),
              onPressed: _delete,
            ),
        ],
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppTheme.gap16),
          child: ResponsiveCenter(
            maxWidth: 600,
            child: FilledButton(
              onPressed: _saving ? null : _save,
              style: FilledButton.styleFrom(
                backgroundColor: const Color(0xFF059669),
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              ),
              child: _saving
                  ? const SizedBox.square(dimension: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
                  : Text(
                      _isEdit ? 'Save Changes' : 'Register Equipment (SOP §4.3)',
                      style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16),
                    ),
            ),
          ),
        ),
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(AppTheme.gap16),
          children: [
            ResponsiveCenter(
              maxWidth: 600,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Verification Banner if existing
                  if (_existing != null) ...[
                    Container(
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: _existing!.isVerified
                            ? const Color(0xFF059669).withValues(alpha: 0.1)
                            : const Color(0xFFF59E0B).withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(
                          color: _existing!.isVerified ? const Color(0xFF059669) : const Color(0xFFF59E0B),
                        ),
                      ),
                      child: Row(
                        children: [
                          Icon(
                            _existing!.isVerified ? Icons.verified_rounded : Icons.pending_rounded,
                            color: _existing!.isVerified ? const Color(0xFF059669) : const Color(0xFFF59E0B),
                            size: 24,
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  _existing!.isVerified ? 'Equipment Verified (SOP §4.3)' : 'Pending Document Audit',
                                  style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    fontSize: 14,
                                    color: _existing!.isVerified ? const Color(0xFF059669) : const Color(0xFFB45309),
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  _existing!.isVerified
                                      ? 'Authorized for automatic bowser fuel dispensing.'
                                      : 'Upload all required documents for immediate review.',
                                  style: TextStyle(
                                    fontSize: 12,
                                    color: isDark ? Colors.grey[300] : const Color(0xFF475569),
                                  ),
                                ),
                              ],
                            ),
                          ),
                          StatusChip.verification(_existing!.verificationStatus),
                        ],
                      ),
                    ),
                    const SizedBox(height: AppTheme.gap16),
                  ],

                  // Live Bangladesh Plate Visualizer Box
                  Container(
                    padding: const EdgeInsets.all(AppTheme.gap20),
                    decoration: BoxDecoration(
                      gradient: const LinearGradient(
                        colors: [Color(0xFF064E3B), Color(0xFF0F172A)],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      ),
                      borderRadius: BorderRadius.circular(22),
                      boxShadow: [
                        BoxShadow(
                          color: const Color(0xFF064E3B).withValues(alpha: 0.25),
                          blurRadius: 16,
                          offset: const Offset(0, 6),
                        ),
                      ],
                    ),
                    child: Column(
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Row(
                              children: [
                                Container(
                                  padding: const EdgeInsets.all(6),
                                  decoration: BoxDecoration(
                                    color: const Color(0xFF10B981).withValues(alpha: 0.2),
                                    shape: BoxShape.circle,
                                  ),
                                  child: Icon(_type.icon, color: const Color(0xFF34D399), size: 18),
                                ),
                                const SizedBox(width: 8),
                                Text(
                                  _nickname.text.trim().isEmpty ? 'Equipment Preview' : _nickname.text.trim(),
                                  style: const TextStyle(
                                    color: Colors.white,
                                    fontWeight: FontWeight.bold,
                                    fontSize: 15,
                                  ),
                                ),
                              ],
                            ),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                              decoration: BoxDecoration(
                                color: const Color(0xFF10B981).withValues(alpha: 0.25),
                                borderRadius: BorderRadius.circular(20),
                              ),
                              child: Text(
                                _fuel.label,
                                style: const TextStyle(color: Color(0xFF34D399), fontWeight: FontWeight.w800, fontSize: 12),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: AppTheme.gap16),
                        LicensePlate(
                          _plate.text.trim().isEmpty ? 'DHAKA METRO-GA 12-3456' : _plate.text.trim(),
                        ),
                        const SizedBox(height: 10),
                        const Text(
                          'SOP §10.3: Driver will scan & match this exact plate before fuel dispensing.',
                          textAlign: TextAlign.center,
                          style: TextStyle(color: Colors.white70, fontSize: 11),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: AppTheme.gap20),

                  // Section 1: Equipment Identification
                  _SectionHeader(title: 'Equipment Details', subtitle: 'Select vehicle category and fuel grade'),
                  const SizedBox(height: AppTheme.gap12),

                  // Vehicle Type Selector Grid
                  const Text('Equipment Category', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
                  const SizedBox(height: AppTheme.gap8),
                  Wrap(
                    spacing: 10,
                    runSpacing: 10,
                    children: [
                      for (final t in VehicleType.values)
                        _TypeSelectChip(
                          type: t,
                          selected: _type == t,
                          onTap: () => setState(() => _type = t),
                        ),
                    ],
                  ),

                  const SizedBox(height: AppTheme.gap16),

                  // Nickname Input
                  TextFormField(
                    controller: _nickname,
                    textCapitalization: TextCapitalization.sentences,
                    onChanged: (_) => setState(() {}),
                    decoration: const InputDecoration(
                      labelText: 'Equipment Nickname',
                      hintText: 'e.g. Executive Prado or DG-01 (Basement)',
                      prefixIcon: Icon(Icons.bookmark_border_rounded),
                    ),
                    validator: (v) => (v == null || v.trim().isEmpty) ? 'Please give this vehicle/generator a name' : null,
                  ),

                  const SizedBox(height: AppTheme.gap14),

                  // Registration Number Input
                  TextFormField(
                    controller: _plate,
                    textCapitalization: TextCapitalization.characters,
                    inputFormatters: [_UpperCaseFormatter()],
                    onChanged: (_) => setState(() {}),
                    decoration: const InputDecoration(
                      labelText: 'Registration / Plate Number (BRTA)',
                      hintText: 'DHAKA METRO-GA 12-3456',
                      prefixIcon: Icon(Icons.confirmation_number_outlined),
                      helperText: 'Must match BRTA Blue Book or Generator Asset Tag',
                    ),
                    validator: (v) => (v == null || v.trim().length < 5) ? 'Enter the full registration number' : null,
                  ),

                  const SizedBox(height: AppTheme.gap20),

                  // Fuel Grade Selection
                  const Text('Approved Fuel Type', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
                  const SizedBox(height: AppTheme.gap8),
                  Row(
                    children: [
                      for (final f in FuelType.values)
                        Expanded(
                          child: Padding(
                            padding: const EdgeInsets.symmetric(horizontal: 4),
                            child: InkWell(
                              onTap: () => setState(() => _fuel = f),
                              borderRadius: BorderRadius.circular(14),
                              child: AnimatedContainer(
                                duration: const Duration(milliseconds: 200),
                                padding: const EdgeInsets.symmetric(vertical: 12),
                                decoration: BoxDecoration(
                                  color: _fuel == f
                                      ? const Color(0xFF059669)
                                      : (isDark ? const Color(0xFF1E293B) : Colors.white),
                                  borderRadius: BorderRadius.circular(14),
                                  border: Border.all(
                                    color: _fuel == f ? const Color(0xFF059669) : (isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
                                    width: _fuel == f ? 2 : 1,
                                  ),
                                ),
                                child: Column(
                                  children: [
                                    Icon(
                                      Icons.local_gas_station_rounded,
                                      color: _fuel == f ? Colors.white : const Color(0xFF059669),
                                      size: 20,
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      f.label,
                                      style: TextStyle(
                                        color: _fuel == f ? Colors.white : (isDark ? Colors.white : const Color(0xFF0F172A)),
                                        fontWeight: FontWeight.w800,
                                        fontSize: 13,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                        ),
                    ],
                  ),

                  const SizedBox(height: AppTheme.gap24),

                  // Section 2: Compliance Documents (SOP §4.3 & §10.3)
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      _SectionHeader(title: 'Compliance & Verification', subtitle: 'BRTA Blue Book & Plate Photo for SOP safety'),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: const Color(0xFF059669).withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(
                          '${_docs.values.where((f) => f != null).length}/3 Uploaded',
                          style: const TextStyle(color: Color(0xFF059669), fontWeight: FontWeight.w800, fontSize: 12),
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: AppTheme.gap12),

                  for (final k in DocumentKind.values) ...[
                    _DocUploadCard(
                      kind: k,
                      fileName: _docs[k],
                      onUpload: () => _attachDocument(k),
                      onDelete: () => setState(() => _docs[k] = null),
                    ),
                    const SizedBox(height: AppTheme.gap10),
                  ],

                  const SizedBox(height: AppTheme.gap32),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.title, required this.subtitle});

  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(title, style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 16)),
        const SizedBox(height: 2),
        Text(subtitle, style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant, fontSize: 12)),
      ],
    );
  }
}

class _TypeSelectChip extends StatelessWidget {
  const _TypeSelectChip({required this.type, required this.selected, required this.onTap});

  final VehicleType type;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 180),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        decoration: BoxDecoration(
          color: selected
              ? const Color(0xFF059669).withValues(alpha: isDark ? 0.25 : 0.12)
              : (isDark ? const Color(0xFF1E293B) : Colors.white),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: selected ? const Color(0xFF059669) : (isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
            width: selected ? 2 : 1,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              type.icon,
              size: 18,
              color: selected ? const Color(0xFF059669) : (isDark ? Colors.grey[400] : const Color(0xFF64748B)),
            ),
            const SizedBox(width: 8),
            Text(
              type.label,
              style: TextStyle(
                fontWeight: selected ? FontWeight.w800 : FontWeight.w600,
                color: selected ? const Color(0xFF059669) : (isDark ? Colors.white : const Color(0xFF1E293B)),
                fontSize: 13,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _DocUploadCard extends StatelessWidget {
  const _DocUploadCard({
    required this.kind,
    required this.fileName,
    required this.onUpload,
    required this.onDelete,
  });

  final DocumentKind kind;
  final String? fileName;
  final VoidCallback onUpload;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    final hasFile = fileName != null;
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: hasFile
            ? const Color(0xFF059669).withValues(alpha: isDark ? 0.12 : 0.05)
            : (isDark ? const Color(0xFF1E293B) : Colors.white),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: hasFile ? const Color(0xFF059669).withValues(alpha: 0.5) : (isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
        ),
      ),
      child: Row(
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: hasFile ? const Color(0xFF059669).withValues(alpha: 0.15) : (isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9)),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(
              kind.isPhoto ? Icons.camera_alt_outlined : Icons.description_outlined,
              color: hasFile ? const Color(0xFF059669) : Colors.grey,
              size: 22,
            ),
          ),
          const SizedBox(width: AppTheme.gap14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  kind.label,
                  style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13.5),
                ),
                const SizedBox(height: 2),
                Text(
                  hasFile ? fileName! : (kind.isPhoto ? 'Required for driver SOP §10 plate scan' : 'Official BRTA document PDF'),
                  style: TextStyle(
                    color: hasFile ? const Color(0xFF059669) : theme.colorScheme.onSurfaceVariant,
                    fontSize: 12,
                    fontWeight: hasFile ? FontWeight.w600 : FontWeight.normal,
                  ),
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
          if (hasFile)
            IconButton(
              tooltip: 'Remove',
              icon: const Icon(Icons.close_rounded, size: 20, color: Color(0xFFDC2626)),
              onPressed: onDelete,
            )
          else
            OutlinedButton.icon(
              onPressed: onUpload,
              style: OutlinedButton.styleFrom(
                minimumSize: const Size(80, 36),
                padding: const EdgeInsets.symmetric(horizontal: 12),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              ),
              icon: const Icon(Icons.cloud_upload_outlined, size: 16),
              label: const Text('Upload', style: TextStyle(fontSize: 12.5)),
            ),
        ],
      ),
    );
  }
}

class _UpperCaseFormatter extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) =>
      newValue.copyWith(text: newValue.text.toUpperCase());
}
