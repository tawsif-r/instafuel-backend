// Contracts between the UI and the data source.
//
// Screens/controllers only know these interfaces. Auth, vehicles, orders,
// payments and corporate use the real API (data/api/).
// providers.dart is the single place to configure them. Nothing in features/ needs to change.

import 'dart:typed_data';

import '../models/corporate.dart';
import '../models/fuel.dart';
import '../models/order.dart';
import '../models/payment.dart';
import '../models/user.dart';
import '../models/vehicle.dart';

abstract class AuthRepository {
  /// [identifier] is an email or phone number.
  Future<AuthSession> login(String identifier, String password);
  Future<AuthSession> register(RegisterRequest request);
  Future<void> logout();
  Future<AppUser> updateProfile({required String fullName, required String phone});

  /// Called on app start with the saved session, so later calls send its token.
  void restoreSession(AuthSession session);
}

abstract class VehicleRepository {
  Future<List<Vehicle>> getVehicles();

  /// Creates the vehicle when its id is empty, otherwise updates it.
  Future<Vehicle> saveVehicle(Vehicle vehicle);
  Future<void> deleteVehicle(String id);

  /// Uploads (or replaces) one document file. Returns the updated vehicle.
  Future<Vehicle> uploadDocument({
    required String vehicleId,
    required DocumentKind kind,
    required List<int> bytes,
    required String fileName,
  });
  Future<Vehicle> deleteDocument({required String vehicleId, required DocumentKind kind});

  /// The stored file, for previewing images in the app.
  Future<Uint8List> downloadDocument({required String vehicleId, required DocumentKind kind});
}

abstract class OrderRepository {
  Future<List<FuelProduct>> getFuelProducts();
  Future<List<Order>> getOrders();
  Future<OrderQuote> quote(OrderDraft draft);
  Future<Order> placeOrder(OrderDraft draft);
  Future<Order> cancelOrder(String id);
  Future<Order> progressDelivery(String id);
}

abstract class PaymentRepository {
  Future<PaymentResult> pay({required String orderId, required PaymentMethod method});
}

abstract class CorporateRepository {
  Future<List<CorporateAffiliation>> getAffiliations();

  /// Requests to join a company with the code the company admin shared.
  Future<CorporateAffiliation> joinCompany(String companyCode);
}

/// Thrown by repositories with a message that is safe to show to the user.
class AppException implements Exception {
  const AppException(this.message);
  final String message;

  @override
  String toString() => message;
}
