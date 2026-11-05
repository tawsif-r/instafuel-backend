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
          title: const Text('Orders'),
          bottom: const TabBar(tabs: [Tab(text: 'Active'), Tab(text: 'Past')]),
        ),
        floatingActionButton: FloatingActionButton.extended(
          onPressed: () => context.push('/new-order'),
          icon: const Icon(Icons.add),
          label: const Text('New order'),
        ),
        body: AsyncValueView(
          value: orders,
          onRetry: () => ref.invalidate(ordersProvider),
          data: (list) => TabBarView(
            children: [
              _OrderList(
                orders: list.where((o) => o.isActive).toList(),
                emptyTitle: 'No active orders',
                emptyMessage: 'Orders you place will show up here until they are delivered.',
              ),
              _OrderList(
                orders: list.where((o) => !o.isActive).toList(),
                emptyTitle: 'No past orders yet',
                emptyMessage: 'Completed and cancelled orders are kept here.',
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
      return EmptyState(icon: Icons.receipt_long_outlined, title: emptyTitle, message: emptyMessage);
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
