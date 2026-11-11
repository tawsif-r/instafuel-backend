import 'dart:typed_data';

import 'package:instafuel_customer/data/repositories.dart';
import 'package:instafuel_customer/models/corporate.dart';
import 'package:instafuel_customer/models/fuel.dart';
import 'package:instafuel_customer/models/order.dart';
import 'package:instafuel_customer/models/payment.dart';
import 'package:instafuel_customer/models/user.dart';
import 'package:instafuel_customer/models/vehicle.dart';

class FakeAuthRepository implements AuthRepository {
  AppUser? loggedInUser;

  @override
  Future<AuthSession> login(String identifier, String password) async {
    if (password == 'wrong-password') {
      throw const AppException('Incorrect email/phone or password.');
    }
    final user = AppUser(
      id: 'test-user-id',
      fullName: 'Ayesha Khan',
      email: identifier.contains('@') ? identifier : 'ayesha@example.com',
      phone: identifier.contains('@') ? '01700000000' : identifier,
      accountType: AccountType.individual,
    );
    loggedInUser = user;
    return AuthSession(
      token: 'fake-jwt-token',
      user: user,
      expiresAt: DateTime.now().add(const Duration(days: 7)),
    );
  }

  @override
  Future<AuthSession> register(RegisterRequest request) async {
    final user = AppUser(
      id: 'new-user-id',
      fullName: request.fullName,
      email: request.email,
      phone: request.phone,
      accountType: request.accountType,
      companyName: request.companyName,
    );
    loggedInUser = user;
    return AuthSession(
      token: 'fake-jwt-token',
      user: user,
      expiresAt: DateTime.now().add(const Duration(days: 7)),
    );
  }

  @override
  Future<void> logout() async {
    loggedInUser = null;
  }

  @override
  Future<AppUser> updateProfile({required String fullName, required String phone}) async {
    final current = loggedInUser ??
        const AppUser(
          id: 'test-user-id',
          fullName: 'Ayesha Khan',
          email: 'ayesha@example.com',
          phone: '01711223344',
          accountType: AccountType.individual,
        );
    final updated = AppUser(
      id: current.id,
      fullName: fullName,
      email: current.email,
      phone: phone,
      accountType: current.accountType,
      companyName: current.companyName,
    );
    loggedInUser = updated;
    return updated;
  }

  @override
  void restoreSession(AuthSession session) {
    loggedInUser = session.user;
  }
}

class FakeVehicleRepository implements VehicleRepository {
  final List<Vehicle> vehicles = [
    const Vehicle(
      id: 'v1',
      nickname: 'Family Car',
      type: VehicleType.car,
      registrationNumber: 'DHAKA METRO-GA 11-2233',
      fuelType: FuelType.octane,
      verificationStatus: VerificationStatus.verified,
    ),
  ];

  @override
  Future<List<Vehicle>> getVehicles() async => List.of(vehicles);

  @override
  Future<Vehicle> saveVehicle(Vehicle vehicle) async {
    vehicles.add(vehicle);
    return vehicle;
  }

  @override
  Future<void> deleteVehicle(String id) async {
    vehicles.removeWhere((v) => v.id == id);
  }

  Vehicle _replaceDoc(String vehicleId, DocumentKind kind, VehicleDocument? doc) {
    final i = vehicles.indexWhere((v) => v.id == vehicleId);
    if (i == -1) throw const AppException('Vehicle not found');
    final docs = [
      for (final d in vehicles[i].documents)
        if (d.kind != kind) d,
      ?doc,
    ];
    return vehicles[i] = vehicles[i].copyWith(documents: docs);
  }

  @override
  Future<Vehicle> uploadDocument({
    required String vehicleId,
    required DocumentKind kind,
    required List<int> bytes,
    required String fileName,
  }) async =>
      _replaceDoc(
        vehicleId,
        kind,
        VehicleDocument(kind: kind, fileName: fileName, hasFile: true, sizeBytes: bytes.length, uploadedAt: DateTime.now()),
      );

  @override
  Future<Vehicle> deleteDocument({required String vehicleId, required DocumentKind kind}) async =>
      _replaceDoc(vehicleId, kind, null);

  @override
  Future<Uint8List> downloadDocument({required String vehicleId, required DocumentKind kind}) async => Uint8List(0);
}

class FakeOrderRepository implements OrderRepository {
  final List<Order> orders = [
    Order(
      id: 'o1',
      orderNumber: 'IF-1043',
      lines: const [
        OrderLine(
          vehicleId: 'v1',
          vehicleName: 'Family Car',
          registrationNumber: 'DHAKA METRO-GA 11-2233',
          fuelType: FuelType.octane,
          litres: 30,
          unitPrice: 135,
        ),
      ],
      address: 'Dhanmondi 27, Dhaka',
      scheduledAt: DateTime.now().add(const Duration(hours: 26)),
      createdAt: DateTime.now().subtract(const Duration(hours: 2)),
      deliveryFee: 150,
      deliveryStatus: DeliveryStatus.inTransit,
      driverName: 'Kamal Hossain',
      truckRegistration: 'DHAKA METRO-SHA 11-0022',
    ),
  ];

  @override
  Future<List<FuelProduct>> getFuelProducts() async => const [
        FuelProduct(type: FuelType.octane, pricePerLitre: 135),
        FuelProduct(type: FuelType.petrol, pricePerLitre: 130),
        FuelProduct(type: FuelType.diesel, pricePerLitre: 114),
      ];

  @override
  Future<List<Order>> getOrders() async => List.of(orders);

  @override
  Future<OrderQuote> quote(OrderDraft draft) async {
    final subtotal = draft.lines.fold<double>(0, (sum, l) => sum + (l.litres * 135));
    return OrderQuote(subtotal: subtotal, deliveryFee: 150);
  }

  @override
  Future<Order> placeOrder(OrderDraft draft) async {
    final order = Order(
      id: 'o-new',
      orderNumber: 'IF-9999',
      lines: [
        for (final l in draft.lines)
          OrderLine(
            vehicleId: l.vehicleId,
            vehicleName: 'Equipment',
            registrationNumber: 'DHAKA METRO-GA 11-2233',
            fuelType: l.fuelType,
            litres: l.litres,
            unitPrice: 135,
          ),
      ],
      address: draft.address,
      scheduledAt: draft.scheduledAt,
      createdAt: DateTime.now(),
      deliveryFee: 150,
    );
    orders.add(order);
    return order;
  }

  @override
  Future<Order> cancelOrder(String id) async {
    final index = orders.indexWhere((o) => o.id == id);
    if (index != -1) {
      final updated = orders[index].copyWith(status: OrderStatus.cancelled);
      orders[index] = updated;
      return updated;
    }
    throw const AppException('Order not found');
  }

  @override
  Future<Order> progressDelivery(String id) async {
    final index = orders.indexWhere((o) => o.id == id);
    if (index != -1) {
      final order = orders[index];
      final nextStatus = switch (order.deliveryStatus) {
        DeliveryStatus.pending => DeliveryStatus.assigned,
        DeliveryStatus.assigned => DeliveryStatus.dispatched,
        DeliveryStatus.dispatched => DeliveryStatus.inTransit,
        _ => DeliveryStatus.delivered,
      };
      final updated = order.copyWith(
        deliveryStatus: nextStatus,
        driverName: order.driverName ?? 'Kamal Hossain',
        truckRegistration: order.truckRegistration ?? 'DHAKA METRO-SHA 11-2045',
        status: nextStatus == DeliveryStatus.delivered ? OrderStatus.completed : order.status,
      );
      orders[index] = updated;
      return updated;
    }
    throw const AppException('Order not found');
  }
}

class FakePaymentRepository implements PaymentRepository {
  @override
  Future<PaymentResult> pay({required String orderId, required PaymentMethod method}) async {
    return PaymentResult(
      orderId: orderId,
      status: PaymentStatus.paid,
      method: method,
      amount: 4200,
      reference: 'TXN123456',
    );
  }
}

class FakeCorporateRepository implements CorporateRepository {
  @override
  Future<List<CorporateAffiliation>> getAffiliations() async => const [
        CorporateAffiliation(
          id: 'c1',
          companyName: 'Northwind Logistics Ltd.',
          role: CorporateRole.member,
          status: AffiliationStatus.active,
          creditLimit: 100000,
          usedCredit: 15000,
        ),
      ];

  @override
  Future<CorporateAffiliation> joinCompany(String companyCode) async {
    return const CorporateAffiliation(
      id: 'c1',
      companyName: 'Northwind Logistics Ltd.',
      role: CorporateRole.member,
      status: AffiliationStatus.active,
      creditLimit: 100000,
      usedCredit: 15000,
    );
  }
}
