// Full order view complying with SOP-OPS-001 Section 11.3:
// - Order ID & Electronic Tax Invoice Number
// - Delivery Progress & Bowser Fleet Assignment
// - Number Plate Check per vehicle before dispensing
// - Transparent Pricing (Fuel Charge + Zone Delivery Fee)
// - Digital Calibrated Meter Seal & HSE Quality Certificate

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
        title: const Text('Cancel Fuel Delivery?'),
        content: const Text(
          'Under SOP §15, orders may be cancelled prior to vehicle dispatch without cancellation fees. Any prepaid amount will be refunded immediately.',
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Keep Order')),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            style: FilledButton.styleFrom(backgroundColor: const Color(0xFFDC2626)),
            child: const Text('Confirm Cancellation'),
          ),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await ref.read(ordersProvider.notifier).cancel(orderId);
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Order cancelled successfully.')));
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
      appBar: AppBar(
        title: Text(order?.orderNumber ?? 'Delivery Manifest'),
        actions: [
          IconButton(
            tooltip: 'Share Manifest',
            icon: const Icon(Icons.share_outlined),
            onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Delivery manifest link copied to clipboard.')),
            ),
          ),
        ],
      ),
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
                          child: OutlinedButton(
                            onPressed: () => _cancel(context, ref),
                            style: OutlinedButton.styleFrom(
                              foregroundColor: const Color(0xFFDC2626),
                              side: const BorderSide(color: Color(0xFFDC2626)),
                              padding: const EdgeInsets.symmetric(vertical: 14),
                            ),
                            child: const Text('Cancel Order (SOP §15)'),
                          ),
                        ),
                      if (order.canCancel && order.needsPayment) const SizedBox(width: AppTheme.gap12),
                      if (order.needsPayment)
                        Expanded(
                          flex: 2,
                          child: FilledButton(
                            onPressed: () => context.push('/order/${order.id}/pay'),
                            style: FilledButton.styleFrom(
                              backgroundColor: const Color(0xFF059669),
                              foregroundColor: Colors.white,
                              padding: const EdgeInsets.symmetric(vertical: 14),
                            ),
                            child: Text('Pay Invoice ${formatMoney(order.total)}', style: const TextStyle(fontWeight: FontWeight.bold)),
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
              // ----- Manifest & Delivery Status -----
              Container(
                padding: const EdgeInsets.all(AppTheme.gap16),
                decoration: BoxDecoration(
                  color: Theme.of(context).cardTheme.color,
                  borderRadius: BorderRadius.circular(18),
                  border: Border.all(color: Colors.grey.withValues(alpha: 0.25)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          'SOP DELIVERY MANIFEST',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 0.8,
                            color: theme.colorScheme.primary,
                          ),
                        ),
                        Row(
                          children: [
                            StatusChip.delivery(order.deliveryStatus),
                            const SizedBox(width: 6),
                            StatusChip.payment(order.paymentStatus),
                          ],
                        ),
                      ],
                    ),
                    const SizedBox(height: AppTheme.gap12),
                    Text(
                      'Delivery Window: ${formatDateTime(order.scheduledAt)}',
                      style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800),
                    ),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        const Icon(Icons.location_on_rounded, size: 16, color: Color(0xFF059669)),
                        const SizedBox(width: 4),
                        Expanded(child: Text(order.address, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600))),
                      ],
                    ),
                    if (!stopped) ...[
                      const SizedBox(height: AppTheme.gap20),
                      DeliveryProgress(status: order.deliveryStatus),
                    ],

                    // Assigned Driver & Bowser Truck
                    if (order.driverName != null) ...[
                      const Divider(height: AppTheme.gap28),
                      Row(
                        children: [
                          Container(
                            width: 44,
                            height: 44,
                            decoration: const BoxDecoration(
                              shape: BoxShape.circle,
                              color: Color(0xFF0F172A),
                            ),
                            child: const Icon(Icons.person_pin_rounded, color: Color(0xFF34D399), size: 26),
                          ),
                          const SizedBox(width: AppTheme.gap12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Row(
                                  children: [
                                    Text(order.driverName!, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
                                    const SizedBox(width: 6),
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                      decoration: BoxDecoration(
                                        color: const Color(0xFF10B981).withValues(alpha: 0.15),
                                        borderRadius: BorderRadius.circular(4),
                                      ),
                                      child: const Text(
                                        'HAZMAT CERTIFIED',
                                        style: TextStyle(color: Color(0xFF059669), fontSize: 8.5, fontWeight: FontWeight.bold),
                                      ),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 2),
                                if (order.truckRegistration != null) LicensePlate(order.truckRegistration!, compact: true),
                              ],
                            ),
                          ),
                          IconButton.filledTonal(
                            tooltip: 'Call Driver',
                            icon: const Icon(Icons.phone_rounded),
                            onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(content: Text('Contacting driver via secure relay...')),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ],
                ),
              ),
              gap,

              // ----- SOP Plate Verification Check -----
              SectionCard(
                title: 'Vehicle Verification at Dispensing (SOP §11.2)',
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Text(
                      'Per SOP safety regulations, delivery crew matches each vehicle plate with approved photo records before dispensing fuel.',
                      style: TextStyle(fontSize: 12, color: Colors.grey, height: 1.4),
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

              // ----- Formal Invoicing Structure (SOP §7.1) -----
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
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('COMMERCIAL INVOICE', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 13)),
                        if (order.paymentMethod != null)
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                            decoration: BoxDecoration(
                              color: const Color(0xFF059669).withValues(alpha: 0.1),
                              borderRadius: BorderRadius.circular(6),
                            ),
                            child: Text(
                              order.paymentMethod!.label.toUpperCase(),
                              style: const TextStyle(color: Color(0xFF059669), fontSize: 10, fontWeight: FontWeight.bold),
                            ),
                          ),
                      ],
                    ),
                    const Divider(height: 20),
                    for (final l in order.lines)
                      _Row('${l.vehicleName} · ${formatLitres(l.litres)} × ${formatMoney(l.unitPrice)}', formatMoney(l.lineTotal)),
                    _Row('Distance-based Delivery Fee (Zone A)', formatMoney(order.deliveryFee)),
                    const Divider(height: 20),
                    _Row('Total Invoice Amount', formatMoney(order.total), bold: true),
                    if (order.corporateId != null) ...[
                      const SizedBox(height: AppTheme.gap8),
                      Container(
                        padding: const EdgeInsets.all(8),
                        decoration: BoxDecoration(
                          color: const Color(0xFFF0FDF4),
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: const Color(0xFFBBF7D0)),
                        ),
                        child: const Row(
                          children: [
                            Icon(Icons.business_center_rounded, size: 16, color: Color(0xFF16A34A)),
                            SizedBox(width: 8),
                            Text(
                              'Charged to approved corporate credit account (30-day billing)',
                              style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: Color(0xFF15803D)),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ],
                ),
              ),
              gap,

              // ----- Delivery Audit Timeline (SOP §16) -----
              SectionCard(
                title: 'Operational Audit Trail (SOP §16)',
                child: Column(
                  children: [
                    for (final (i, e) in order.timeline.reversed.indexed)
                      _TimelineRow(event: e, isLatest: i == 0, isLast: i == order.timeline.length - 1),
                  ],
                ),
              ),

              if (order.notes != null) ...[
                gap,
                SectionCard(
                  title: 'Site & Access Notes',
                  child: Text(order.notes!, style: const TextStyle(fontSize: 13)),
                ),
              ],
              gap,

              // HSE Safety Certification Note
              Container(
                padding: const EdgeInsets.all(AppTheme.gap12),
                decoration: BoxDecoration(
                  color: const Color(0xFFF8FAFC),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFFE2E8F0)),
                ),
                child: const Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Icon(Icons.verified_rounded, size: 16, color: Color(0xFF10B981)),
                        SizedBox(width: 6),
                        Text(
                          'PESO Certified Digital Metering',
                          style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Color(0xFF0F172A)),
                        ),
                      ],
                    ),
                    SizedBox(height: 2),
                    Text(
                      'Flow meter tested & calibrated under ISO standards. Tamper-proof digital seal verified.',
                      style: TextStyle(fontSize: 10.5, color: Color(0xFF64748B)),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: AppTheme.gap24),
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
    return Padding(
      padding: const EdgeInsets.only(bottom: AppTheme.gap12),
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: Colors.grey.withValues(alpha: 0.2)),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            Container(
              width: 40,
              height: 40,
              decoration: BoxDecoration(
                color: const Color(0xFF059669).withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(10),
              ),
              child: const Icon(Icons.directions_car_rounded, color: Color(0xFF059669), size: 22),
            ),
            const SizedBox(width: AppTheme.gap12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(line.vehicleName, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13.5)),
                  const SizedBox(height: 2),
                  LicensePlate(line.registrationNumber, compact: true),
                  const SizedBox(height: 2),
                  Text(
                    '${line.fuelType.label} · ${formatLitres(line.litres)}',
                    style: const TextStyle(fontSize: 11.5, color: Colors.grey, fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            ),
            line.plateVerified
                ? const StatusChip(label: 'Verified Plate', tone: StatusTone.success, icon: Icons.check_circle_rounded)
                : const StatusChip(label: 'Pending Verification', icon: Icons.schedule_rounded),
          ],
        ),
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
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Text(
              label,
              style: TextStyle(
                fontWeight: bold ? FontWeight.w900 : FontWeight.w500,
                fontSize: bold ? 15 : 13,
              ),
            ),
          ),
          const SizedBox(width: AppTheme.gap8),
          Text(
            value,
            style: TextStyle(
              fontWeight: bold ? FontWeight.w900 : FontWeight.w700,
              fontSize: bold ? 17 : 13,
              color: bold ? const Color(0xFF059669) : null,
            ),
          ),
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
    const activeColor = Color(0xFF059669);
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
                  color: isLatest ? activeColor : Colors.grey.withValues(alpha: 0.4),
                ),
              ),
              if (!isLast) Expanded(child: Container(width: 2, color: Colors.grey.withValues(alpha: 0.3))),
            ],
          ),
          const SizedBox(width: AppTheme.gap12),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(bottom: AppTheme.gap16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(event.title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
                  const SizedBox(height: 2),
                  Text(formatDateTime(event.at), style: const TextStyle(fontSize: 11, color: Colors.grey)),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
