// Contracts between the UI and the data source.
//
// Screens/controllers only know these interfaces. Auth, vehicles, orders,
// payments and corporate use the real API (data/api/) when MOCK_API is false.
// providers.dart is the single place to configure them. Nothing in features/ needs to change.

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

  /// Called on app start with the saved session, so later calls send its token.
  void restoreSession(AuthSession session);
}

abstract class VehicleRepository {
  Future<List<Vehicle>> getVehicles();

  /// Creates the vehicle when its id is empty, otherwise updates it.
  Future<Vehicle> saveVehicle(Vehicle vehicle);
  Future<void> deleteVehicle(String id);
}

abstract class OrderRepository {
  Future<List<FuelProduct>> getFuelProducts();
  Future<List<Order>> getOrders();
  Future<OrderQuote> quote(OrderDraft draft);
  Future<Order> placeOrder(OrderDraft draft);
  Future<Order> cancelOrder(String id);
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
