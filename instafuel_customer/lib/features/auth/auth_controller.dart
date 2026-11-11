// Holds the logged-in user (null = logged out). The session (JWT + user) is
// saved in secure storage so the user stays logged in after a restart.

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/providers.dart';
import '../../models/user.dart';

final authControllerProvider = NotifierProvider<AuthController, AppUser?>(AuthController.new);

class AuthController extends Notifier<AppUser?> {
  @override
  AppUser? build() {
    // Any API call that comes back 401 means the token is no longer valid.
    ref.read(apiClientProvider).onUnauthorized = () {
      if (state != null) logout();
    };

    final saved = ref.read(initialSessionProvider);
    if (saved == null) return null;
    if (saved.isExpired) {
      ref.read(sessionStoreProvider).clear();
      return null;
    }
    ref.read(authRepositoryProvider).restoreSession(saved);
    return saved.user;
  }

  // Errors (AppException) are thrown to the screen so it can show them.
  Future<void> login(String identifier, String password) async {
    final session = await ref.read(authRepositoryProvider).login(identifier, password);
    await _start(session);
  }

  Future<void> register(RegisterRequest request) async {
    final session = await ref.read(authRepositoryProvider).register(request);
    await _start(session);
  }

  Future<void> updateProfile({required String fullName, required String phone}) async {
    final updated = await ref.read(authRepositoryProvider).updateProfile(
      fullName: fullName,
      phone: phone,
    );
    final saved = await ref.read(sessionStoreProvider).read();
    if (saved != null) {
      final newSession = AuthSession(
        token: saved.token,
        expiresAt: saved.expiresAt,
        user: updated,
      );
      await ref.read(sessionStoreProvider).save(newSession);
    }
    state = updated;
  }

  Future<void> logout() async {
    await ref.read(authRepositoryProvider).logout();
    await ref.read(sessionStoreProvider).clear();
    state = null;
  }

  Future<void> _start(AuthSession session) async {
    await ref.read(sessionStoreProvider).save(session);
    state = session.user;
  }
}
