import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme.dart';
import '../../core/widgets/async_value_view.dart';
import '../../core/widgets/empty_state.dart';
import '../../core/widgets/responsive_center.dart';
import '../../models/order.dart';
import 'order_tile.dart';
import 'orders_controller.dart';

class OrdersScreen extends ConsumerWidget {
  const OrdersScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final orders = ref.watch(ordersProvider);

    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Fuel Orders & Dispatches'),
          bottom: const TabBar(
            indicatorColor: Color(0xFF059669),
            labelColor: Color(0xFF059669),
            unselectedLabelColor: Colors.grey,
            labelStyle: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
            tabs: [
              Tab(
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.local_shipping_outlined, size: 18),
                    SizedBox(width: 8),
                    Text('Active Deliveries'),
                  ],
                ),
              ),
              Tab(
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.history_rounded, size: 18),
                    SizedBox(width: 8),
                    Text('Completed & Past'),
                  ],
                ),
              ),
            ],
          ),
        ),
        floatingActionButton: FloatingActionButton.extended(
          onPressed: () => context.push('/new-order'),
          backgroundColor: const Color(0xFF059669),
          foregroundColor: Colors.white,
          icon: const Icon(Icons.add_rounded),
          label: const Text('Schedule Refuel', style: TextStyle(fontWeight: FontWeight.bold)),
        ),
        body: AsyncValueView(
          value: orders,
          onRetry: () => ref.invalidate(ordersProvider),
          data: (list) => TabBarView(
            children: [
              _OrderList(
                orders: list.where((o) => o.isActive).toList(),
                emptyTitle: 'No Active Fuel Deliveries',
                emptyMessage: 'Booked doorstep fuel orders with scheduled slots appear here until delivered.',
              ),
              _OrderList(
                orders: list.where((o) => !o.isActive).toList(),
                emptyTitle: 'No Past Delivery Records',
                emptyMessage: 'Completed delivery manifests and settled fuel invoices will be archived here.',
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _OrderList extends ConsumerWidget {
  const _OrderList({required this.orders, required this.emptyTitle, required this.emptyMessage});

  final List<Order> orders;
  final String emptyTitle;
  final String emptyMessage;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (orders.isEmpty) {
      return EmptyState(
        icon: Icons.receipt_long_rounded,
        title: emptyTitle,
        message: emptyMessage,
      );
    }
    return RefreshIndicator(
      onRefresh: () => ref.refresh(ordersProvider.future),
      child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(AppTheme.gap16, AppTheme.gap16, AppTheme.gap16, 96),
        itemCount: orders.length,
        separatorBuilder: (_, _) => const SizedBox(height: AppTheme.gap12),
        itemBuilder: (_, i) => ResponsiveCenter(child: OrderTile(order: orders[i])),
      ),
    );
  }
}
