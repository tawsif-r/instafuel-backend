// Holds the logged-in user (null = logged out). The session is saved in
// shared_preferences so the user stays logged in after a restart.

import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/providers.dart';
import '../../models/user.dart';

final authControllerProvider = NotifierProvider<AuthController, AppUser?>(AuthController.new);

class AuthController extends Notifier<AppUser?> {
  static const _sessionKey = 'session_user';

  @override
  AppUser? build() {
    final saved = ref.read(sharedPreferencesProvider).getString(_sessionKey);
    if (saved == null) return null;
    try {
      final user = AppUser.fromJson(jsonDecode(saved) as Map<String, dynamic>);
      ref.read(authRepositoryProvider).restoreSession(user);
      return user;
    } catch (_) {
      return null; // corrupted session -> just log in again
    }
  }

  // Errors (AppException) are thrown to the screen so it can show them.
  Future<void> login(String identifier, String password) async {
    final user = await ref.read(authRepositoryProvider).login(identifier, password);
    await _save(user);
  }

  Future<void> register(RegisterRequest request) async {
    final user = await ref.read(authRepositoryProvider).register(request);
    await _save(user);
  }

  Future<void> logout() async {
    await ref.read(authRepositoryProvider).logout();
    await ref.read(sharedPreferencesProvider).remove(_sessionKey);
    state = null;
  }

  Future<void> _save(AppUser user) async {
    await ref.read(sharedPreferencesProvider).setString(_sessionKey, jsonEncode(user.toJson()));
    state = user;
  }
}
