// Home tab: live tracker for the current delivery, order CTA, prices,
// vehicles needing attention and recent orders.

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../core/widgets/delivery_progress.dart';
import '../../core/widgets/license_plate.dart';
import '../../core/widgets/responsive_center.dart';
import '../../core/widgets/section_card.dart';
import '../../core/widgets/status_chip.dart';
import '../../models/order.dart';
import '../auth/auth_controller.dart';
import '../orders/order_tile.dart';
import '../orders/orders_controller.dart';
import '../vehicles/vehicles_controller.dart';

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authControllerProvider);
    final orders = ref.watch(ordersProvider).value ?? [];
    final vehicles = ref.watch(vehiclesProvider).value ?? [];
    final prices = ref.watch(fuelProductsProvider).value ?? [];
    final loading = ref.watch(ordersProvider).isLoading && orders.isEmpty;

    final active = orders.where((o) => o.isActive).toList()
      ..sort((a, b) => a.scheduledAt.compareTo(b.scheduledAt));
    // Prefer a delivery that is already moving; otherwise the next scheduled one.
    final tracked = active.where((o) => o.deliveryStatus != DeliveryStatus.pending).firstOrNull ?? active.firstOrNull;
    final needsAttention = vehicles.where((v) => !v.isVerified).length;
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Hello, ${user?.firstName ?? ''}', style: theme.textTheme.titleLarge),
            Text(
              'Where should we deliver today?',
              style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
          ],
        ),
        actions: [
          IconButton(
            tooltip: 'Notifications',
            icon: const Icon(Icons.notifications_none),
            onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Notifications are coming soon.')),
            ),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () {
          ref.invalidate(vehiclesProvider);
          return ref.refresh(ordersProvider.future);
        },
        child: ListView(
          padding: const EdgeInsets.all(AppTheme.gap16),
          children: [
            ResponsiveCenter(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (loading)
                    const Padding(padding: EdgeInsets.all(AppTheme.gap32), child: Center(child: CircularProgressIndicator()))
                  else if (tracked != null) ...[
                    _TrackerCard(order: tracked),
                    const SizedBox(height: AppTheme.gap16),
                  ],
                  _OrderFuelCard(hasActive: active.isNotEmpty),
                  const SizedBox(height: AppTheme.gap16),
                  if (prices.isNotEmpty) ...[
                    Text("Today's prices", style: theme.textTheme.titleMedium),
                    const SizedBox(height: AppTheme.gap8),
                    Row(
                      children: [
                        for (final p in prices) ...[
                          Expanded(
                            child: Card(
                              child: Padding(
                                padding: const EdgeInsets.all(AppTheme.gap12),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(p.type.label, style: theme.textTheme.labelLarge),
                                    const SizedBox(height: AppTheme.gap4),
                                    Text(formatMoney(p.pricePerLitre), style: theme.textTheme.titleMedium),
                                    Text('per litre', style: theme.textTheme.bodySmall),
                                  ],
                                ),
                              ),
                            ),
                          ),
                          if (p != prices.last) const SizedBox(width: AppTheme.gap8),
                        ],
                      ],
                    ),
                    const SizedBox(height: AppTheme.gap16),
                  ],
                  if (needsAttention > 0) ...[
                    Card(
                      color: theme.colorScheme.tertiaryContainer,
                      child: ListTile(
                        leading: Icon(Icons.assignment_late_outlined, color: theme.colorScheme.onTertiaryContainer),
                        title: Text(
                          '$needsAttention vehicle${needsAttention == 1 ? '' : 's'} not verified',
                          style: TextStyle(color: theme.colorScheme.onTertiaryContainer),
                        ),
                        subtitle: Text(
                          'Add the missing documents to order fuel for them.',
                          style: TextStyle(color: theme.colorScheme.onTertiaryContainer),
                        ),
                        trailing: Icon(Icons.chevron_right, color: theme.colorScheme.onTertiaryContainer),
                        onTap: () => context.go('/vehicles'),
                      ),
                    ),
                    const SizedBox(height: AppTheme.gap16),
                  ],
                  if (orders.isNotEmpty) ...[
                    Row(
                      children: [
                        Expanded(child: Text('Recent orders', style: theme.textTheme.titleMedium)),
                        TextButton(onPressed: () => context.go('/orders'), child: const Text('See all')),
                      ],
                    ),
                    for (final o in orders.take(3)) ...[OrderTile(order: o), const SizedBox(height: AppTheme.gap12)],
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _TrackerCard extends StatelessWidget {
  const _TrackerCard({required this.order});

  final Order order;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final moving = order.deliveryStatus == DeliveryStatus.dispatched || order.deliveryStatus == DeliveryStatus.inTransit;

    return SectionCard(
      title: 'Tracking ${order.orderNumber}',
      trailing: StatusChip.delivery(order.deliveryStatus),
      child: InkWell(
        onTap: () => context.push('/order/${order.id}'),
        borderRadius: BorderRadius.circular(AppTheme.radius),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              moving ? 'Arriving ${formatRelative(order.scheduledAt)}' : 'Scheduled ${formatDateTime(order.scheduledAt)}',
              style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: AppTheme.gap4),
            Text(
              '${formatLitres(order.totalLitres)} for ${order.lines.length} vehicle${order.lines.length == 1 ? '' : 's'} · ${order.address}',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
            const SizedBox(height: AppTheme.gap16),
            DeliveryProgress(status: order.deliveryStatus),
            if (order.driverName != null) ...[
              const Divider(height: AppTheme.gap32),
              Row(
                children: [
                  const CircleAvatar(child: Icon(Icons.person)),
                  const SizedBox(width: AppTheme.gap12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(order.driverName!, style: theme.textTheme.titleSmall),
                        const SizedBox(height: AppTheme.gap4),
                        if (order.truckRegistration != null) LicensePlate(order.truckRegistration!, compact: true),
                      ],
                    ),
                  ),
                  IconButton.filledTonal(
                    tooltip: 'Call driver',
                    icon: const Icon(Icons.call_outlined),
                    onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Calling the driver is coming soon.')),
                    ),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _OrderFuelCard extends StatelessWidget {
  const _OrderFuelCard({required this.hasActive});

  final bool hasActive;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      color: theme.colorScheme.primaryContainer,
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.gap16),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    hasActive ? 'Need more fuel?' : 'Order fuel',
                    style: theme.textTheme.titleLarge?.copyWith(color: theme.colorScheme.onPrimaryContainer),
                  ),
                  const SizedBox(height: AppTheme.gap4),
                  Text(
                    'Octane, petrol or diesel to one or more vehicles. Book at least 24 hours ahead.',
                    style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onPrimaryContainer),
                  ),
                  const SizedBox(height: AppTheme.gap12),
                  FilledButton.icon(
                    onPressed: () => context.push('/new-order'),
                    icon: const Icon(Icons.add),
                    label: const Text('Start order'),
                  ),
                ],
              ),
            ),
            const SizedBox(width: AppTheme.gap12),
            Icon(Icons.local_gas_station, size: 64, color: theme.colorScheme.primary),
          ],
        ),
      ),
    );
  }
}
