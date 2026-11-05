// Mock orders for offline use (MOCK_API=true). The real one is ApiOrderRepository.

import '../../models/fuel.dart';
import '../../models/order.dart';
import '../repositories.dart';
import 'mock_database.dart';

class MockOrderRepository implements OrderRepository {
  MockOrderRepository(this._db);
  final MockDatabase _db;

  @override
  Future<List<FuelProduct>> getFuelProducts() async {
    await fakeDelay(300);
    return _db.products;
  }

  @override
  Future<List<Order>> getOrders() async {
    await fakeDelay();
    return List.of(_db.myOrders)..sort((a, b) => b.createdAt.compareTo(a.createdAt));
  }

  @override
  Future<OrderQuote> quote(OrderDraft draft) async {
    await fakeDelay(400);
    final subtotal = draft.lines.fold<double>(0, (sum, l) => sum + l.litres * _db.priceOf(l.fuelType));
    return OrderQuote(subtotal: subtotal, deliveryFee: MockDatabase.deliveryFee);
  }

  @override
  Future<Order> placeOrder(OrderDraft draft) async {
    await fakeDelay(900);
    if (draft.lines.isEmpty) throw const AppException('Add at least one vehicle.');
    if (draft.scheduledAt.isBefore(DateTime.now().add(orderLeadTime))) {
      throw const AppException('Delivery must be at least 24 hours from now.');
    }

    final lines = <OrderLine>[];
    for (final l in draft.lines) {
      final vehicle = _db.myVehicles.where((v) => v.id == l.vehicleId).firstOrNull;
      if (vehicle == null || !vehicle.isVerified) {
        throw const AppException('Only verified vehicles can be ordered for.');
      }
      lines.add(OrderLine(
        vehicleId: vehicle.id,
        vehicleName: vehicle.nickname,
        registrationNumber: vehicle.registrationNumber,
        fuelType: l.fuelType,
        litres: l.litres,
        unitPrice: _db.priceOf(l.fuelType),
      ));
    }

    final now = DateTime.now();
    final id = _db.newId();
    final order = Order(
      id: id,
      orderNumber: 'IF-${1000 + int.parse(id)}',
      lines: lines,
      address: draft.address,
      scheduledAt: draft.scheduledAt,
      createdAt: now,
      deliveryFee: MockDatabase.deliveryFee,
      corporateId: draft.corporateId,
      notes: draft.notes,
      timeline: [OrderEvent('Order placed', now)],
    );
    _db.myOrders.add(order);
    return order;
  }

  @override
  Future<Order> cancelOrder(String id) async {
    await fakeDelay();
    final index = _db.myOrders.indexWhere((o) => o.id == id);
    if (index == -1) throw const AppException('Order not found.');
    final order = _db.myOrders[index];
    if (!order.canCancel) throw const AppException('This order can no longer be cancelled.');

    final cancelled = order.copyWith(
      status: OrderStatus.cancelled,
      deliveryStatus: DeliveryStatus.cancelled,
      timeline: [...order.timeline, OrderEvent('Cancelled by you', DateTime.now())],
    );
    _db.myOrders[index] = cancelled;
    return cancelled;
  }
}
