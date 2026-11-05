// Full order view: progress, driver, per-vehicle plate check, payment, timeline.

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../core/widgets/async_value_view.dart';
import '../../core/widgets/delivery_progress.dart';
import '../../core/widgets/empty_state.dart';
import '../../core/widgets/license_plate.dart';
import '../../core/widgets/responsive_center.dart';
import '../../core/widgets/section_card.dart';
import '../../core/widgets/status_chip.dart';
import '../../models/order.dart';
import '../../models/vehicle.dart';
import '../vehicles/vehicles_controller.dart';
import 'orders_controller.dart';

class OrderDetailScreen extends ConsumerWidget {
  const OrderDetailScreen({super.key, required this.orderId});

  final String orderId;

  Future<void> _cancel(BuildContext context, WidgetRef ref) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cancel this order?'),
        content: const Text('The order will be cancelled and any payment refunded.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Keep order')),
          FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Cancel order')),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await ref.read(ordersProvider.notifier).cancel(orderId);
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Order cancelled')));
      }
    } catch (e) {
      if (context.mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final orderValue = ref.watch(orderByIdProvider(orderId));
    final order = orderValue.value;

    return Scaffold(
      appBar: AppBar(title: Text(order?.orderNumber ?? 'Order')),
      body: AsyncValueView(
        value: orderValue,
        onRetry: () => ref.invalidate(ordersProvider),
        data: (order) => order == null
            ? const EmptyState(icon: Icons.search_off, title: 'Order not found')
            : _Body(order: order),
      ),
      bottomNavigationBar: order == null || (!order.needsPayment && !order.canCancel)
          ? null
          : SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(AppTheme.gap16),
                child: ResponsiveCenter(
                  child: Row(
                    children: [
                      if (order.canCancel)
                        Expanded(
                          child: OutlinedButton(onPressed: () => _cancel(context, ref), child: const Text('Cancel')),
                        ),
                      if (order.canCancel && order.needsPayment) const SizedBox(width: AppTheme.gap12),
                      if (order.needsPayment)
                        Expanded(
                          flex: 2,
                          child: FilledButton(
                            onPressed: () => context.push('/order/${order.id}/pay'),
                            child: Text('Pay ${formatMoney(order.total)}'),
                          ),
                        ),
                    ],
                  ),
                ),
              ),
            ),
    );
  }
}

class _Body extends ConsumerWidget {
  const _Body({required this.order});

  final Order order;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final vehicles = ref.watch(vehiclesProvider).value ?? [];
    final stopped = order.deliveryStatus == DeliveryStatus.cancelled || order.deliveryStatus == DeliveryStatus.failed;
    const gap = SizedBox(height: AppTheme.gap16);

    return ListView(
      padding: const EdgeInsets.all(AppTheme.gap16),
      children: [
        ResponsiveCenter(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // ----- Status -----
              SectionCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Wrap(
                      spacing: AppTheme.gap8,
                      runSpacing: AppTheme.gap8,
                      children: [
                        StatusChip.delivery(order.deliveryStatus),
                        StatusChip.payment(order.paymentStatus),
                      ],
                    ),
                    const SizedBox(height: AppTheme.gap12),
                    Text(formatDateTime(order.scheduledAt), style: theme.textTheme.titleLarge),
                    const SizedBox(height: AppTheme.gap4),
                    Row(
                      children: [
                        Icon(Icons.location_on_outlined, size: 18, color: theme.colorScheme.onSurfaceVariant),
                        const SizedBox(width: AppTheme.gap4),
                        Expanded(child: Text(order.address, style: theme.textTheme.bodyMedium)),
                      ],
                    ),
                    if (!stopped) ...[
                      const SizedBox(height: AppTheme.gap16),
                      DeliveryProgress(status: order.deliveryStatus),
                    ],
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
                                Text('Your driver', style: theme.textTheme.bodySmall),
                              ],
                            ),
                          ),
                          if (order.truckRegistration != null) LicensePlate(order.truckRegistration!, compact: true),
                        ],
                      ),
                    ],
                  ],
                ),
              ),
              gap,

              // ----- Plate check per vehicle -----
              SectionCard(
                title: 'Vehicle check at delivery',
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      'The driver matches each number plate with the photo on file before fuelling. '
                      'Only these vehicles can receive fuel.',
                      style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                    ),
                    const SizedBox(height: AppTheme.gap12),
                    for (final line in order.lines)
                      _VehicleLine(
                        line: line,
                        vehicle: vehicles.where((v) => v.id == line.vehicleId).firstOrNull,
                      ),
                  ],
                ),
              ),
              gap,

              // ----- Payment -----
              SectionCard(
                title: 'Payment',
                trailing: order.paymentMethod == null ? null : Text(order.paymentMethod!.label),
                child: Column(
                  children: [
                    for (final l in order.lines)
                      _Row('${l.vehicleName} · ${formatLitres(l.litres)} × ${formatMoney(l.unitPrice)}', formatMoney(l.lineTotal)),
                    _Row('Delivery fee', formatMoney(order.deliveryFee)),
                    const Divider(),
                    _Row('Total', formatMoney(order.total), bold: true),
                    if (order.corporateId != null) ...[
                      const SizedBox(height: AppTheme.gap8),
                      const Row(
                        children: [
                          Icon(Icons.business_outlined, size: 18),
                          SizedBox(width: AppTheme.gap8),
                          Text('Billed to your company'),
                        ],
                      ),
                    ],
                  ],
                ),
              ),
              gap,

              // ----- Timeline -----
              SectionCard(
                title: 'Activity',
                child: Column(
                  children: [
                    for (final (i, e) in order.timeline.reversed.indexed)
                      _TimelineRow(event: e, isLatest: i == 0, isLast: i == order.timeline.length - 1),
                  ],
                ),
              ),
              if (order.notes != null) ...[
                gap,
                SectionCard(title: 'Notes for driver', child: Text(order.notes!)),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class _VehicleLine extends StatelessWidget {
  const _VehicleLine({required this.line, required this.vehicle});

  final OrderLine line;
  final Vehicle? vehicle;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final hasPhoto = vehicle?.document(DocumentKind.numberPlatePhoto).isUploaded ?? false;

    return Padding(
      padding: const EdgeInsets.only(bottom: AppTheme.gap12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Placeholder for the number plate photo thumbnail (real image later).
          Container(
            width: 64,
            height: 48,
            decoration: BoxDecoration(
              color: theme.colorScheme.surfaceContainerHighest,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(
              hasPhoto ? Icons.image_outlined : Icons.hide_image_outlined,
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(width: AppTheme.gap12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(line.vehicleName, style: theme.textTheme.titleSmall),
                const SizedBox(height: AppTheme.gap4),
                LicensePlate(line.registrationNumber, compact: true),
                const SizedBox(height: AppTheme.gap4),
                Text('${line.fuelType.label} · ${formatLitres(line.litres)}', style: theme.textTheme.bodySmall),
              ],
            ),
          ),
          line.plateVerified
              ? const StatusChip(label: 'Plate matched', tone: StatusTone.success, icon: Icons.check)
              : const StatusChip(label: 'Not checked yet', icon: Icons.schedule),
        ],
      ),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row(this.label, this.value, {this.bold = false});

  final String label;
  final String value;
  final bool bold;

  @override
  Widget build(BuildContext context) {
    final style = bold ? Theme.of(context).textTheme.titleMedium : Theme.of(context).textTheme.bodyMedium;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(child: Text(label, style: style)),
          const SizedBox(width: AppTheme.gap8),
          Text(value, style: style),
        ],
      ),
    );
  }
}

class _TimelineRow extends StatelessWidget {
  const _TimelineRow({required this.event, required this.isLatest, required this.isLast});

  final OrderEvent event;
  final bool isLatest;
  final bool isLast;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Column(
            children: [
              Container(
                width: 12,
                height: 12,
                margin: const EdgeInsets.only(top: 4),
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: isLatest ? scheme.primary : scheme.outlineVariant,
                ),
              ),
              if (!isLast) Expanded(child: Container(width: 2, color: scheme.outlineVariant)),
            ],
          ),
          const SizedBox(width: AppTheme.gap12),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(bottom: AppTheme.gap16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(event.title, style: Theme.of(context).textTheme.bodyMedium),
                  Text(formatDateTime(event.at), style: Theme.of(context).textTheme.bodySmall),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
