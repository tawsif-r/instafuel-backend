// The ONE place that configures repositories for the application.
// All customer repositories communicate directly with Instafuel.Api over HTTP/REST.

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

final authRepositoryProvider = Provider<AuthRepository>(
  (ref) => ApiAuthRepository(ref.watch(apiClientProvider)),
);

final vehicleRepositoryProvider = Provider<VehicleRepository>(
  (ref) => ApiVehicleRepository(ref.watch(apiClientProvider)),
);

final orderRepositoryProvider = Provider<OrderRepository>(
  (ref) => ApiOrderRepository(ref.watch(apiClientProvider)),
);

final paymentRepositoryProvider = Provider<PaymentRepository>(
  (ref) => ApiPaymentRepository(ref.watch(apiClientProvider)),
);

final corporateRepositoryProvider = Provider<CorporateRepository>(
  (ref) => ApiCorporateRepository(ref.watch(apiClientProvider)),
);
