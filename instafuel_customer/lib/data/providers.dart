// The ONE place that decides which implementation each repository uses.
// Auth, vehicles, orders, payments and corporate talk to Instafuel.Api (or mocks with MOCK_API=true).

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../core/config.dart';
import '../models/user.dart';
import 'api/api_auth_repository.dart';
import 'api/api_client.dart';
import 'api/api_corporate_repository.dart';
import 'api/api_order_repository.dart';
import 'api/api_payment_repository.dart';
import 'api/api_vehicle_repository.dart';
import 'mock/mock_auth_repository.dart';
import 'mock/mock_corporate_repository.dart';
import 'mock/mock_database.dart';
import 'mock/mock_order_repository.dart';
import 'mock/mock_payment_repository.dart';
import 'mock/mock_vehicle_repository.dart';
import 'repositories.dart';
import 'session_store.dart';

/// Loaded in main() and injected with overrideWithValue.
final sharedPreferencesProvider = Provider<SharedPreferences>(
  (ref) => throw UnimplementedError('Override sharedPreferencesProvider in main()'),
);

final sessionStoreProvider = Provider<SessionStore>((ref) => SessionStore());

/// Session saved from the last run. Secure storage is async, so main() reads it
/// before runApp and overrides this; null means logged out.
final initialSessionProvider = Provider<AuthSession?>((ref) => null);

/// One HTTP client for the whole app, so the token set at login is shared.
final apiClientProvider = Provider<ApiClient>((ref) => ApiClient(baseUrl: apiBaseUrl));

/// In-memory data shared by all mock repositories (resets on app restart).
final mockDatabaseProvider = Provider<MockDatabase>((ref) => MockDatabase.seeded());

final authRepositoryProvider = Provider<AuthRepository>((ref) => useMockApi
    ? MockAuthRepository(ref.watch(mockDatabaseProvider))
    : ApiAuthRepository(ref.watch(apiClientProvider)));

final vehicleRepositoryProvider = Provider<VehicleRepository>((ref) => useMockApi
    ? MockVehicleRepository(ref.watch(mockDatabaseProvider))
    : ApiVehicleRepository(ref.watch(apiClientProvider)));

final orderRepositoryProvider = Provider<OrderRepository>((ref) => useMockApi
    ? MockOrderRepository(ref.watch(mockDatabaseProvider))
    : ApiOrderRepository(ref.watch(apiClientProvider)));

final paymentRepositoryProvider = Provider<PaymentRepository>((ref) => useMockApi
    ? MockPaymentRepository(ref.watch(mockDatabaseProvider))
    : ApiPaymentRepository(ref.watch(apiClientProvider)));

final corporateRepositoryProvider = Provider<CorporateRepository>((ref) => useMockApi
    ? MockCorporateRepository(ref.watch(mockDatabaseProvider))
    : ApiCorporateRepository(ref.watch(apiClientProvider)));
