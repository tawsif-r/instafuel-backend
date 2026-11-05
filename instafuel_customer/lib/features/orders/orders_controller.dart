import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/providers.dart';
import '../../models/fuel.dart';
import '../../models/order.dart';
import '../auth/auth_controller.dart';

final ordersProvider = AsyncNotifierProvider<OrdersController, List<Order>>(OrdersController.new);

/// One order by id, taken from the loaded list (null if it doesn't exist).
final orderByIdProvider = Provider.family<AsyncValue<Order?>, String>((ref, id) {
  return ref.watch(ordersProvider).whenData((orders) => orders.where((o) => o.id == id).firstOrNull);
});

final fuelProductsProvider = FutureProvider<List<FuelProduct>>(
  (ref) => ref.read(orderRepositoryProvider).getFuelProducts(),
);

class OrdersController extends AsyncNotifier<List<Order>> {
  @override
  Future<List<Order>> build() async {
    // Watching auth reloads this list when the user logs in/out.
    if (ref.watch(authControllerProvider) == null) return [];
    return ref.read(orderRepositoryProvider).getOrders();
  }

  Future<Order> place(OrderDraft draft) async {
    final order = await ref.read(orderRepositoryProvider).placeOrder(draft);
    ref.invalidateSelf();
    return order;
  }

  Future<void> cancel(String id) async {
    await ref.read(orderRepositoryProvider).cancelOrder(id);
    ref.invalidateSelf();
  }

  /// Called after a payment so the order shows its new status.
  void refresh() => ref.invalidateSelf();
}
