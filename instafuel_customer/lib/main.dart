import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'app.dart';
import 'data/providers.dart';
import 'data/session_store.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // Loaded once up front so theme + session can be read synchronously.
  final prefs = await SharedPreferences.getInstance();
  final sessionStore = SessionStore();
  final session = await sessionStore.read();

  runApp(
    ProviderScope(
      overrides: [
        sharedPreferencesProvider.overrideWithValue(prefs),
        sessionStoreProvider.overrideWithValue(sessionStore),
        initialSessionProvider.overrideWithValue(session),
      ],
      child: const InstafuelApp(),
    ),
  );
}
