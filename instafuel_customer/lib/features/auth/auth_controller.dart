// Holds the logged-in user (null = logged out). The session (JWT + user) is
// saved in secure storage so the user stays logged in after a restart.

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/providers.dart';
import '../../models/corporate.dart';
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
    _useMockDataFor(saved.user);
    return saved.user;
  }

  // Errors (AppException) are thrown to the screen so it can show them.
  Future<void> login(String identifier, String password) async {
    final session = await ref.read(authRepositoryProvider).login(identifier, password);
    await _start(session);
  }

  Future<void> register(RegisterRequest request) async {
    final session = await ref.read(authRepositoryProvider).register(request);
    _useMockDataFor(session.user);

    // TODO(api): the API stores only the account type for now. Until company
    // tables exist, a corporate sign-up shows as a pending (mock) company link.
    if (request.accountType == AccountType.corporate && request.companyName != null) {
      final db = ref.read(mockDatabaseProvider);
      db.myAffiliations.add(CorporateAffiliation(
        id: db.newId(),
        companyName: request.companyName!,
        role: CorporateRole.manager,
        status: AffiliationStatus.pending,
      ));
    }
    await _start(session);
  }

  Future<void> logout() async {
    await ref.read(authRepositoryProvider).logout();
    await ref.read(sessionStoreProvider).clear();
    ref.read(mockDatabaseProvider).currentUserId = null;
    state = null;
  }

  Future<void> _start(AuthSession session) async {
    _useMockDataFor(session.user);
    await ref.read(sessionStoreProvider).save(session);
    state = session.user;
  }

  // TODO(api): remove once vehicles/orders/payments/corporate use the API.
  // Those are still mocked and keep their data per user id.
  void _useMockDataFor(AppUser user) => ref.read(mockDatabaseProvider).currentUserId = user.id;
}
