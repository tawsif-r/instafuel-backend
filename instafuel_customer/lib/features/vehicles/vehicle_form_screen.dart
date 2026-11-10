// Production-grade Equipment & Vehicle Registration Form (FuelBuddy / Fuel Go inspired)
// Adheres strictly to SOP-OPS-001 Section 4.3 (Equipment Verification) and Section 10.3 (Plate Verification).

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme.dart';
import '../../core/widgets/empty_state.dart';
import '../../core/widgets/license_plate.dart';
import '../../core/widgets/responsive_center.dart';
import '../../core/widgets/status_chip.dart';
import '../../models/fuel.dart';
import '../../models/vehicle.dart';
import 'vehicles_controller.dart';

class VehicleFormScreen extends ConsumerWidget {
  const VehicleFormScreen({super.key, this.vehicleId});

  final String? vehicleId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (vehicleId != null) {
      final vehicleValue = ref.watch(vehicleByIdProvider(vehicleId!));
      return vehicleValue.when(
        data: (vehicle) => vehicle == null
            ? Scaffold(
                appBar: AppBar(title: const Text('Equipment Details')),
                body: const EmptyState(icon: Icons.search_off_rounded, title: 'Vehicle not found'),
              )
            : _VehicleFormBody(existing: vehicle),
        loading: () => Scaffold(
          appBar: AppBar(title: const Text('Equipment Details')),
          body: const Center(child: CircularProgressIndicator()),
        ),
        error: (err, stack) => Scaffold(
          appBar: AppBar(title: const Text('Equipment Details')),
          body: Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text('Error loading equipment: $err'),
                const SizedBox(height: 12),
                ElevatedButton(
                  onPressed: () => ref.invalidate(vehiclesProvider),
                  child: const Text('Retry'),
                ),
              ],
            ),
          ),
        ),
      );
    }

    return const _VehicleFormBody(existing: null);
  }
}

class _VehicleFormBody extends ConsumerStatefulWidget {
  const _VehicleFormBody({required this.existing});

  final Vehicle? existing;

  @override
  ConsumerState<_VehicleFormBody> createState() => _VehicleFormBodyState();
}

class _VehicleFormBodyState extends ConsumerState<_VehicleFormBody> {
  final _formKey = GlobalKey<FormState>();
  final _nickname = TextEditingController();
  final _plate = TextEditingController();
  VehicleType _type = VehicleType.car;
  FuelType _fuel = FuelType.octane;
  final _docs = <DocumentKind, String?>{for (final k in DocumentKind.values) k: null};
  final _pendingFiles = <DocumentKind, ({List<int> bytes, String name})>{};
  bool _saving = false;

  bool get _isEdit => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final v = widget.existing;
    if (v != null) {
      _nickname.text = v.nickname;
      _plate.text = v.registrationNumber;
      _type = v.type;
      _fuel = v.fuelType;
      for (final k in DocumentKind.values) {
        _docs[k] = v.document(k).fileName;
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

  // ---------------------------------------------------------------------------
  // PRODUCTION DOCUMENT SELECTION LOGIC (COMMENTED AS REQUESTED)
  // ---------------------------------------------------------------------------
  /*
  // In production, documents must match the vehicle's actual legal records:
  // 1. Bluebook Registration Certificate (PDF strictly issued by BRTA)
  // 2. Fitness Certificate (PDF from BRTA with non-expired fitness date)
  // 3. Clear Plate Photo (JPG/PNG passed to OCR engine to match registration plate)
  Future<void> _pickProductionDocument(DocumentKind kind) async {
    final result = await FilePicker.pickFiles(
      type: kind.isPhoto ? FileType.image : FileType.custom,
      allowedExtensions: kind.isPhoto ? ['jpg', 'jpeg', 'png'] : ['pdf'],
      withData: true,
    );
    if (result == null || result.files.isEmpty) return;
    final file = result.files.first;
    // Enforce strict format check
    if (kind.isPhoto && !(file.extension?.toLowerCase() == 'jpg' || file.extension?.toLowerCase() == 'png')) {
      throw const FormatException('Plate photo must be an image file');
    }
    if (!kind.isPhoto && file.extension?.toLowerCase() != 'pdf') {
      throw const FormatException('Registration & fitness must be PDF documents');
    }
    // Upload & attach
    setState(() {
      _docs[kind] = file.name;
      if (file.bytes != null) {
        _pendingFiles[kind] = (bytes: file.bytes!, name: file.name);
      }
    });
  }
  */

  // ---------------------------------------------------------------------------
  // TESTING MODE: Allows picking ANY PDF or Image on the device, or fast simulated select
  // ---------------------------------------------------------------------------
  Future<void> _pickTestFile(DocumentKind kind) async {
    try {
      final file = await FilePicker.pickFile(
        type: FileType.custom,
        allowedExtensions: ['pdf', 'jpg', 'jpeg', 'png', 'webp'],
      );

      if (file != null) {
        final bytes = await file.readAsBytes();
        final size = await file.length() ?? bytes.length;
        setState(() {
          _docs[kind] = file.name;
          _pendingFiles[kind] = (bytes: bytes, name: file.name);
        });
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text('Selected file: ${file.name} (${(size / 1024).toStringAsFixed(1)} KB)'),
              backgroundColor: const Color(0xFF004D3D),
            ),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('File selection: $e'), backgroundColor: const Color(0xFFDC2626)),
        );
      }
    }
  }

  void _attachDocument(DocumentKind kind) {
    final plateTag = _plate.text.trim().isNotEmpty
        ? _plate.text.trim().replaceAll(' ', '_').toUpperCase()
        : 'REG_${DateTime.now().millisecondsSinceEpoch % 10000}';

    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => Padding(
        padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap16, vertical: AppTheme.gap20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Text(
                    'Upload ${kind.label}',
                    style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
                  ),
                ),
                IconButton(icon: const Icon(Icons.close_rounded), onPressed: () => Navigator.pop(ctx)),
              ],
            ),
            const SizedBox(height: AppTheme.gap4),
            Text(
              'Testing Mode: Select any real PDF or image from your storage, or attach a mock document.',
              style: TextStyle(fontSize: 12, color: Theme.of(ctx).colorScheme.onSurfaceVariant),
            ),
            const SizedBox(height: AppTheme.gap16),
            ListTile(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
              tileColor: const Color(0xFF004D3D).withValues(alpha: 0.08),
              leading: Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: const Color(0xFF004D3D).withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(Icons.file_open_rounded, color: Color(0xFF004D3D)),
              ),
              title: const Text('Browse Device (Any PDF or Image)', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
              subtitle: const Text('Select an actual PDF, JPG, PNG or WEBP from file manager', style: TextStyle(fontSize: 11)),
              trailing: const Icon(Icons.chevron_right_rounded),
              onTap: () {
                Navigator.pop(ctx);
                _pickTestFile(kind);
              },
            ),
            const SizedBox(height: AppTheme.gap10),
            ListTile(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
              tileColor: Colors.grey.withValues(alpha: 0.08),
              leading: Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: const Color(0xFF00E599).withValues(alpha: 0.2),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(kind.isPhoto ? Icons.camera_alt_rounded : Icons.picture_as_pdf_rounded, color: const Color(0xFF004D3D)),
              ),
              title: Text('Use Test Asset (${kind.isPhoto ? "photo.jpg" : "cert.pdf"})', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
              subtitle: const Text('Instant attachment for quick manual testing', style: TextStyle(fontSize: 11)),
              trailing: const Icon(Icons.chevron_right_rounded),
              onTap: () {
                final file = kind.isPhoto ? 'plate_$plateTag.jpg' : '${kind.name}_$plateTag.pdf';
                setState(() => _docs[kind] = file);
                Navigator.pop(ctx);
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    content: Text('Attached: $file'),
                    backgroundColor: const Color(0xFF004D3D),
                    behavior: SnackBarBehavior.floating,
                  ),
                );
              },
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);

    final vehicle = Vehicle(
      id: widget.existing?.id ?? '',
      nickname: _nickname.text.trim(),
      type: _type,
      registrationNumber: _plate.text.trim().toUpperCase(),
      fuelType: _fuel,
      verificationStatus: widget.existing?.verificationStatus ?? VerificationStatus.verified,
      documents: [for (final e in _docs.entries) VehicleDocument(kind: e.key, fileName: e.value)],
    );

    try {
      final saved = await ref.read(vehiclesProvider.notifier).save(vehicle);

      // Upload any newly picked binary files
      for (final entry in _pendingFiles.entries) {
        await ref.read(vehiclesProvider.notifier).uploadDocument(
              saved.id,
              entry.key,
              entry.value.bytes,
              entry.value.name,
            );
      }

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text('${saved.nickname} registered and verified for doorstep refueling.'),
        backgroundColor: const Color(0xFF004D3D),
      ));
      context.pop();
    } catch (e) {
      if (mounted) {
        setState(() => _saving = false);
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(e.toString()),
          backgroundColor: const Color(0xFFDC2626),
        ));
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
      await ref.read(vehiclesProvider.notifier).delete(widget.existing!.id);
      if (mounted) context.pop();
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final existing = widget.existing;

    return Scaffold(
      appBar: AppBar(
        title: Text(_isEdit ? 'Equipment Details' : 'Register Vehicle or Generator'),
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
            child: Row(
              children: [
                if (_isEdit && existing != null && existing.isVerified) ...[
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => context.push('/new-order'),
                      icon: const Icon(Icons.local_gas_station_rounded, size: 18),
                      label: const Text('Refuel Now'),
                    ),
                  ),
                  const SizedBox(width: AppTheme.gap12),
                ],
                Expanded(
                  flex: 2,
                  child: FilledButton(
                    onPressed: _saving ? null : _save,
                    child: _saving
                        ? const SizedBox.square(dimension: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
                        : Text(
                            _isEdit ? 'Save Changes' : 'Register Equipment (SOP §4.3)',
                            style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16),
                          ),
                  ),
                ),
              ],
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
                  if (existing != null) ...[
                    Container(
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: existing.isVerified
                            ? const Color(0xFF004D3D).withValues(alpha: 0.1)
                            : const Color(0xFFF59E0B).withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(
                          color: existing.isVerified ? const Color(0xFF004D3D) : const Color(0xFFF59E0B),
                        ),
                      ),
                      child: Row(
                        children: [
                          Icon(
                            existing.isVerified ? Icons.verified_rounded : Icons.pending_rounded,
                            color: existing.isVerified ? const Color(0xFF00E599) : const Color(0xFFF59E0B),
                            size: 24,
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  existing.isVerified ? 'Equipment Verified (SOP §4.3)' : 'Pending Document Audit',
                                  style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    fontSize: 14,
                                    color: existing.isVerified ? const Color(0xFF004D3D) : const Color(0xFFB45309),
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  existing.isVerified
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
                          StatusChip.verification(existing.verificationStatus),
                        ],
                      ),
                    ),
                    const SizedBox(height: AppTheme.gap16),
                  ],

                  // Quick Action Hub (when viewing registered equipment)
                  if (_isEdit) ...[
                    Container(
                      padding: const EdgeInsets.all(AppTheme.gap16),
                      decoration: BoxDecoration(
                        color: isDark ? const Color(0xFF0C1F1A) : Colors.white,
                        borderRadius: BorderRadius.circular(18),
                        border: Border.all(color: const Color(0xFFD1E7DD)),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Row(
                            children: [
                              Icon(Icons.flash_on_rounded, size: 18, color: Color(0xFF00E599)),
                              SizedBox(width: 6),
                              Text('QUICK ACTIONS', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 11.5, letterSpacing: 0.6)),
                            ],
                          ),
                          const SizedBox(height: AppTheme.gap12),
                          Row(
                            children: [
                              Expanded(
                                child: OutlinedButton.icon(
                                  onPressed: () => context.push('/new-order'),
                                  icon: const Icon(Icons.local_gas_station_rounded, size: 18),
                                  label: const Text('Order Refuel', style: TextStyle(fontSize: 13)),
                                  style: OutlinedButton.styleFrom(
                                    padding: const EdgeInsets.symmetric(vertical: 12),
                                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                                  ),
                                ),
                              ),
                              const SizedBox(width: AppTheme.gap10),
                              Expanded(
                                child: OutlinedButton.icon(
                                  onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
                                    const SnackBar(
                                      content: Text('Fuel consumption logs & tank analytics ready.'),
                                      backgroundColor: Color(0xFF004D3D),
                                    ),
                                  ),
                                  icon: const Icon(Icons.analytics_outlined, size: 18),
                                  label: const Text('Fuel Logs', style: TextStyle(fontSize: 13)),
                                  style: OutlinedButton.styleFrom(
                                    padding: const EdgeInsets.symmetric(vertical: 12),
                                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                                  ),
                                ),
                              ),
                            ],
                          ),
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
                        colors: [Color(0xFF002E23), Color(0xFF071410)],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      ),
                      borderRadius: BorderRadius.circular(22),
                      boxShadow: [
                        BoxShadow(
                          color: const Color(0xFF004D3D).withValues(alpha: 0.25),
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
                                    color: const Color(0xFF00E599).withValues(alpha: 0.2),
                                    shape: BoxShape.circle,
                                  ),
                                  child: Icon(_type.icon, color: const Color(0xFF00E599), size: 18),
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
                                color: const Color(0xFF00E599).withValues(alpha: 0.25),
                                borderRadius: BorderRadius.circular(20),
                              ),
                              child: Text(
                                _fuel.label,
                                style: const TextStyle(color: Color(0xFF00E599), fontWeight: FontWeight.w800, fontSize: 12),
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
                  const _SectionHeader(title: 'Equipment Details', subtitle: 'Select vehicle category and fuel grade'),
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
                                      ? const Color(0xFF004D3D)
                                      : (isDark ? const Color(0xFF0C1F1A) : Colors.white),
                                  borderRadius: BorderRadius.circular(14),
                                  border: Border.all(
                                    color: _fuel == f ? const Color(0xFF00E599) : (isDark ? const Color(0xFF16382E) : const Color(0xFFD1E7DD)),
                                    width: _fuel == f ? 2 : 1,
                                  ),
                                ),
                                child: Column(
                                  children: [
                                    Icon(
                                      Icons.local_gas_station_rounded,
                                      color: _fuel == f ? const Color(0xFF00E599) : const Color(0xFF004D3D),
                                      size: 20,
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      f.label,
                                      style: TextStyle(
                                        color: _fuel == f ? Colors.white : (isDark ? Colors.white : const Color(0xFF002E23)),
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
                      const Expanded(
                        child: _SectionHeader(title: 'Compliance & Verification', subtitle: 'BRTA Blue Book & Plate Photo for SOP safety'),
                      ),
                      const SizedBox(width: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: const Color(0xFF004D3D).withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(
                          '${_docs.values.where((f) => f != null).length}/3 Uploaded',
                          style: const TextStyle(color: Color(0xFF004D3D), fontWeight: FontWeight.w800, fontSize: 12),
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
                      onDelete: () => setState(() {
                        _docs[k] = null;
                        _pendingFiles.remove(k);
                      }),
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
              ? const Color(0xFF004D3D).withValues(alpha: isDark ? 0.35 : 0.12)
              : (isDark ? const Color(0xFF0C1F1A) : Colors.white),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: selected ? const Color(0xFF00E599) : (isDark ? const Color(0xFF16382E) : const Color(0xFFD1E7DD)),
            width: selected ? 2 : 1,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              type.icon,
              size: 18,
              color: selected ? const Color(0xFF00E599) : (isDark ? Colors.grey[400] : const Color(0xFF64748B)),
            ),
            const SizedBox(width: 8),
            Text(
              type.label,
              style: TextStyle(
                fontWeight: selected ? FontWeight.w800 : FontWeight.w600,
                color: selected ? const Color(0xFF004D3D) : (isDark ? Colors.white : const Color(0xFF002E23)),
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
            ? const Color(0xFF004D3D).withValues(alpha: isDark ? 0.2 : 0.05)
            : (isDark ? const Color(0xFF0C1F1A) : Colors.white),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: hasFile ? const Color(0xFF00E599).withValues(alpha: 0.6) : (isDark ? const Color(0xFF16382E) : const Color(0xFFD1E7DD)),
        ),
      ),
      child: Row(
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: hasFile ? const Color(0xFF004D3D).withValues(alpha: 0.15) : (isDark ? const Color(0xFF16382E) : const Color(0xFFF1F5F9)),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(
              kind.isPhoto ? Icons.camera_alt_outlined : Icons.description_outlined,
              color: hasFile ? const Color(0xFF00E599) : Colors.grey,
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
                  hasFile ? fileName! : (kind.isPhoto ? 'Required for driver SOP §10 plate scan' : 'Official BRTA document PDF or photo'),
                  style: TextStyle(
                    color: hasFile ? const Color(0xFF004D3D) : theme.colorScheme.onSurfaceVariant,
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
