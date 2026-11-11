import 'package:flutter/foundation.dart';

/// Base URL for backend API.
/// Injected at compile-time from `.env` file via:
///   --dart-define-from-file=.env
/// or via:
///   --dart-define=API_BASE_URL=http://localhost:5033
const _apiBaseUrlFromEnv = String.fromEnvironment('API_BASE_URL');

/// Where Instafuel.Api runs.
String get apiBaseUrl {
  // If provided via .env or --dart-define, use it directly (works for all platforms)
  if (_apiBaseUrlFromEnv.isNotEmpty) return _apiBaseUrlFromEnv;

  // Fallback defaults if no .env was provided during run/build:
  if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
    // Android emulator reaches host machine through 10.0.2.2
    return 'http://10.0.2.2:5033';
  }
  return 'http://localhost:5033';
}
