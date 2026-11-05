// Build-time settings, passed with --dart-define:
//   flutter run --dart-define=API_BASE_URL=http://192.168.0.10:5033
//   flutter run --dart-define=MOCK_API=true    (offline, demo account + sample data)

import 'package:flutter/foundation.dart';

const _apiBaseUrlOverride = String.fromEnvironment('API_BASE_URL');

/// Use the in-memory mock repositories instead of the API for auth, vehicles,
/// orders and payments (switched together because mock data only matches mock users).
const useMockApi = bool.fromEnvironment('MOCK_API');

/// Where Instafuel.Api runs. Defaults match `dotnet run` (http profile, port 5033).
String get apiBaseUrl {
  if (_apiBaseUrlOverride.isNotEmpty) return _apiBaseUrlOverride;
  // The Android emulator reaches the host machine's localhost through 10.0.2.2.
  if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) return 'http://10.0.2.2:5033';
  return 'http://localhost:5033';
}
