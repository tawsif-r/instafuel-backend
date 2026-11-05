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

    return Scaffold(
      appBar: AppBar(title: const Text('My vehicles')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/vehicle/new'),
        icon: const Icon(Icons.add),
        label: const Text('Add vehicle'),
      ),
      body: AsyncValueView(
        value: vehicles,
        onRetry: () => ref.invalidate(vehiclesProvider),
        data: (list) => list.isEmpty
            ? const EmptyState(
                icon: Icons.directions_car_outlined,
                title: 'No vehicles yet',
                message: 'Register the vehicles you want fuelled. '
                    'The driver checks the number plate before every delivery.',
              )
            : RefreshIndicator(
                onRefresh: () => ref.refresh(vehiclesProvider.future),
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(AppTheme.gap16, AppTheme.gap16, AppTheme.gap16, 96),
                  itemCount: list.length,
                  separatorBuilder: (_, _) => const SizedBox(height: AppTheme.gap12),
                  itemBuilder: (_, i) => ResponsiveCenter(child: _VehicleCard(vehicle: list[i])),
                ),
              ),
      ),
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

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push('/vehicle/${vehicle.id}'),
        child: Padding(
          padding: const EdgeInsets.all(AppTheme.gap16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  CircleAvatar(
                    backgroundColor: theme.colorScheme.secondaryContainer,
                    child: Icon(vehicle.type.icon, color: theme.colorScheme.onSecondaryContainer),
                  ),
                  const SizedBox(width: AppTheme.gap12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(vehicle.nickname, style: theme.textTheme.titleMedium),
                        Text(
                          '${vehicle.type.label} · ${vehicle.fuelType.label}',
                          style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                        ),
                      ],
                    ),
                  ),
                  StatusChip.verification(vehicle.verificationStatus),
                ],
              ),
              if (vehicle.rejectionReason != null) ...[
                const SizedBox(height: AppTheme.gap8),
                Text(
                  'Rejected: ${vehicle.rejectionReason}',
                  style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.error),
                ),
              ],
              const SizedBox(height: AppTheme.gap12),
              LicensePlate(vehicle.registrationNumber),
              const SizedBox(height: AppTheme.gap12),
              Row(
                children: [
                  Expanded(
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(4),
                      child: LinearProgressIndicator(value: vehicle.uploadedCount / total, minHeight: 6),
                    ),
                  ),
                  const SizedBox(width: AppTheme.gap12),
                  Text('${vehicle.uploadedCount}/$total documents', style: theme.textTheme.bodySmall),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
