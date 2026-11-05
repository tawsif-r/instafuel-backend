// Offline auth for tests and demos (run with --dart-define=MOCK_API=true).
// The app normally uses ApiAuthRepository.

import '../../models/user.dart';
import '../repositories.dart';
import 'mock_database.dart';

class MockAuthRepository implements AuthRepository {
  MockAuthRepository(this._db);
  final MockDatabase _db;

  @override
  Future<AuthSession> login(String identifier, String password) async {
    await fakeDelay(800);
    final id = identifier.trim().toLowerCase();
    final user = _db.users.where((u) => u.email.toLowerCase() == id || u.phone == id).firstOrNull;

    if (user == null || _db.passwords[user.id] != password) {
      throw const AppException('Incorrect email/phone or password.');
    }
    return _session(user);
  }

  @override
  Future<AuthSession> register(RegisterRequest request) async {
    await fakeDelay(1000);
    final email = request.email.trim().toLowerCase();
    if (_db.users.any((u) => u.email.toLowerCase() == email)) {
      throw const AppException('An account with this email already exists.');
    }
    if (_db.users.any((u) => u.phone == request.phone)) {
      throw const AppException('An account with this phone number already exists.');
    }

    final user = AppUser(
      id: _db.newId(),
      fullName: request.fullName.trim(),
      email: email,
      phone: request.phone,
      accountType: request.accountType,
      companyName: request.companyName,
    );
    _db.users.add(user);
    _db.passwords[user.id] = request.password;
    return _session(user);
  }

  @override
  Future<void> logout() async {}

  @override
  void restoreSession(AuthSession session) {
    // The mock forgets registered users on restart, so add them back (with empty data).
    if (!_db.users.any((u) => u.id == session.user.id)) _db.users.add(session.user);
  }

  AuthSession _session(AppUser user) => AuthSession(
        token: 'mock-token-${user.id}',
        expiresAt: DateTime.now().add(const Duration(days: 7)),
        user: user,
      );
}
