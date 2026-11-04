// The ONE place that decides which implementation each repository uses.
// To go live with the API, replace e.g. `MockAuthRepository(db)` with
// `ApiAuthRepository(httpClient)` here.

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'mock/mock_auth_repository.dart';
import 'mock/mock_corporate_repository.dart';
import 'mock/mock_database.dart';
import 'mock/mock_order_repository.dart';
import 'mock/mock_payment_repository.dart';
import 'mock/mock_vehicle_repository.dart';
import 'repositories.dart';

/// Loaded in main() and injected with overrideWithValue.
final sharedPreferencesProvider = Provider<SharedPreferences>(
  (ref) => throw UnimplementedError('Override sharedPreferencesProvider in main()'),
);

/// In-memory data shared by all mock repositories (resets on app restart).
final mockDatabaseProvider = Provider<MockDatabase>((ref) => MockDatabase.seeded());

final authRepositoryProvider =
    Provider<AuthRepository>((ref) => MockAuthRepository(ref.watch(mockDatabaseProvider)));

final vehicleRepositoryProvider =
    Provider<VehicleRepository>((ref) => MockVehicleRepository(ref.watch(mockDatabaseProvider)));

final orderRepositoryProvider =
    Provider<OrderRepository>((ref) => MockOrderRepository(ref.watch(mockDatabaseProvider)));

final paymentRepositoryProvider =
    Provider<PaymentRepository>((ref) => MockPaymentRepository(ref.watch(mockDatabaseProvider)));

final corporateRepositoryProvider =
    Provider<CorporateRepository>((ref) => MockCorporateRepository(ref.watch(mockDatabaseProvider)));
