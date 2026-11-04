// Keeps the logged-in session (JWT + user) in secure storage: Android
// Keystore-backed on Android, WebCrypto-encrypted localStorage on web.

import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../models/user.dart';

class SessionStore {
  static const _key = 'auth_session';
  final _storage = const FlutterSecureStorage();

  /// Null when there is no saved session or it can't be read.
  Future<AuthSession?> read() async {
    try {
      final saved = await _storage.read(key: _key);
      if (saved == null) return null;
      return AuthSession.fromJson(jsonDecode(saved) as Map<String, dynamic>);
    } catch (_) {
      return null; // corrupted or unreadable -> just log in again
    }
  }

  // Storage failures must not block login/logout: worst case the user logs in again next launch.
  Future<void> save(AuthSession session) async {
    try {
      await _storage.write(key: _key, value: jsonEncode(session.toJson()));
    } catch (e) {
      debugPrint('SessionStore.save failed: $e');
    }
  }

  Future<void> clear() async {
    try {
      await _storage.delete(key: _key);
    } catch (e) {
      debugPrint('SessionStore.clear failed: $e');
    }
  }
}
