// Production-grade Equipment Garage Screen (FuelBuddy / Fuel Go inspired)
// Complies with SOP-OPS-001 Section 2 (Scope & Equipment) & Section 4.3 (Equipment Verification)

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme.dart';
import '../../core/widgets/async_value_view.dart';
import '../../core/widgets/empty_state.dart';
import '../../core/widgets/license_plate.dart';
import '../../core/widgets/responsive_center.dart';
import '../../core/widgets/status_chip.dart';
import '../../models/vehicle.dart';
import 'vehicles_controller.dart';

class VehiclesScreen extends ConsumerWidget {
  const VehiclesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final vehicles = ref.watch(vehiclesProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Garage & Generators'),
        actions: [
          IconButton(
            tooltip: 'SOP Equipment Standards',
            icon: const Icon(Icons.info_outline_rounded),
            onPressed: () => showDialog(
              context: context,
              builder: (ctx) => AlertDialog(
                title: const Text('Equipment Verification (SOP §4.3)'),
                content: const Text(
                  'Per petroleum safety laws and SOP guidelines, all private vehicles, commercial transports, and backup generators must have verified registration documents and clean plate photos prior to fuel dispensing.',
                ),
                actions: [
                  TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Understood')),
                ],
              ),
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/vehicle/new'),
        backgroundColor: const Color(0xFF004D3D),
        foregroundColor: Colors.white,
        icon: const Icon(Icons.add_rounded),
        label: const Text('Add Vehicle / Generator', style: TextStyle(fontWeight: FontWeight.bold)),
      ),
      body: AsyncValueView(
        value: vehicles,
        onRetry: () => ref.invalidate(vehiclesProvider),
        data: (list) {
          if (list.isEmpty) {
            return EmptyState(
              icon: Icons.directions_car_filled_rounded,
              title: 'No vehicles or generators registered',
              message: 'Register your private vehicle, commercial truck, or building backup generator to order doorstep fuel.',
              action: FilledButton.icon(
                onPressed: () => context.push('/vehicle/new'),
                style: FilledButton.styleFrom(backgroundColor: const Color(0xFF004D3D)),
                icon: const Icon(Icons.add),
                label: const Text('Register First Equipment'),
              ),
            );
          }

          final verifiedCount = list.where((v) => v.isVerified).length;

          return RefreshIndicator(
            onRefresh: () => ref.refresh(vehiclesProvider.future),
            child: ListView(
              padding: const EdgeInsets.fromLTRB(AppTheme.gap16, AppTheme.gap16, AppTheme.gap16, 96),
              children: [
                ResponsiveCenter(
                  maxWidth: 680,
                  child: Column(
                    children: [
                      // Fleet Summary Overview Header Card
                      Container(
                        padding: const EdgeInsets.all(AppTheme.gap16),
                        decoration: BoxDecoration(
                          gradient: const LinearGradient(
                            colors: [Color(0xFF064E3B), Color(0xFF0B1320)],
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                          ),
                          borderRadius: BorderRadius.circular(20),
                          boxShadow: [
                            BoxShadow(
                              color: const Color(0xFF064E3B).withValues(alpha: 0.25),
                              blurRadius: 12,
                              offset: const Offset(0, 4),
                            ),
                          ],
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceAround,
                          children: [
                            _SummaryStat(
                              icon: Icons.directions_car_rounded,
                              label: 'Total Fleet',
                              value: '${list.length}',
                              accent: Colors.white,
                            ),
                            Container(width: 1, height: 36, color: Colors.white24),
                            _SummaryStat(
                              icon: Icons.verified_rounded,
                              label: 'Verified (SOP)',
                              value: '$verifiedCount',
                              accent: const Color(0xFF00E599),
                            ),
                            Container(width: 1, height: 36, color: Colors.white24),
                            _SummaryStat(
                              icon: Icons.local_gas_station_rounded,
                              label: 'Ready to Refuel',
                              value: '$verifiedCount',
                              accent: const Color(0xFF10B981),
                            ),
                          ],
                        ),
                      ),

                      const SizedBox(height: AppTheme.gap20),

                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text(
                            'Registered Assets',
                            style: TextStyle(fontWeight: FontWeight.w900, fontSize: 16),
                          ),
                          Text(
                            '${list.length} item(s)',
                            style: TextStyle(color: theme.colorScheme.onSurfaceVariant, fontSize: 13),
                          ),
                        ],
                      ),

                      const SizedBox(height: AppTheme.gap12),

                      for (final vehicle in list) ...[
                        _VehicleCard(vehicle: vehicle),
                        const SizedBox(height: AppTheme.gap12),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _SummaryStat extends StatelessWidget {
  const _SummaryStat({required this.icon, required this.label, required this.value, required this.accent});

  final IconData icon;
  final String label;
  final String value;
  final Color accent;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, color: accent, size: 16),
            const SizedBox(width: 6),
            Text(
              value,
              style: TextStyle(color: accent, fontWeight: FontWeight.w900, fontSize: 18),
            ),
          ],
        ),
        const SizedBox(height: 2),
        Text(
          label,
          style: const TextStyle(color: Colors.white70, fontSize: 11, fontWeight: FontWeight.w500),
        ),
      ],
    );
  }
}

class _VehicleCard extends StatelessWidget {
  const _VehicleCard({required this.vehicle});

  final Vehicle vehicle;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final total = DocumentKind.values.length;
    final isDark = theme.brightness == Brightness.dark;

    return Container(
      decoration: BoxDecoration(
        color: isDark ? const Color(0xFF1E293B) : Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(
          color: vehicle.isVerified
              ? const Color(0xFF10B981).withValues(alpha: 0.35)
              : (isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
          width: vehicle.isVerified ? 1.5 : 1,
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 8,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: () => context.push('/vehicle/${vehicle.id}'),
          borderRadius: BorderRadius.circular(18),
          child: Padding(
            padding: const EdgeInsets.all(AppTheme.gap16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      width: 48,
                      height: 48,
                      decoration: BoxDecoration(
                        color: const Color(0xFF004D3D).withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(14),
                      ),
                      child: Icon(vehicle.type.icon, color: const Color(0xFF004D3D), size: 26),
                    ),
                    const SizedBox(width: AppTheme.gap14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Expanded(
                                child: Text(
                                  vehicle.nickname,
                                  style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16),
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                              StatusChip.verification(vehicle.verificationStatus),
                            ],
                          ),
                          const SizedBox(height: 4),
                          LicensePlate(vehicle.registrationNumber, compact: true),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppTheme.gap16),
                const Divider(height: 1),
                const SizedBox(height: AppTheme.gap12),
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: const Color(0xFF004D3D).withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.local_gas_station_rounded, size: 14, color: Color(0xFF004D3D)),
                          const SizedBox(width: 4),
                          Text(
                            vehicle.fuelType.label,
                            style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 12, color: Color(0xFF004D3D)),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 8),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(
                        '${vehicle.uploadedCount}/$total docs attached',
                        style: TextStyle(
                          fontSize: 11.5,
                          fontWeight: FontWeight.w600,
                          color: isDark ? Colors.grey[300] : const Color(0xFF475569),
                        ),
                      ),
                    ),
                    const Spacer(),
                    if (vehicle.isVerified)
                      FilledButton.icon(
                        onPressed: () => context.push('/new-order'),
                        style: FilledButton.styleFrom(
                          backgroundColor: const Color(0xFF004D3D),
                          foregroundColor: Colors.white,
                          minimumSize: const Size(100, 34),
                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                        ),
                        icon: const Icon(Icons.bolt_rounded, size: 16),
                        label: const Text('Refuel', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 12.5)),
                      )
                    else
                      TextButton.icon(
                        onPressed: () => context.push('/vehicle/${vehicle.id}'),
                        style: TextButton.styleFrom(
                          foregroundColor: const Color(0xFFD97706),
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        ),
                        icon: const Icon(Icons.upload_file_rounded, size: 16),
                        label: const Text('Add Docs', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 12)),
                      ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
