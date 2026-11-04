// Mock auth. Replace with ApiAuthRepository once the API has
// /auth/register and /auth/login endpoints (none exist yet).

import '../../models/corporate.dart';
import '../../models/user.dart';
import '../repositories.dart';
import 'mock_database.dart';

class MockAuthRepository implements AuthRepository {
  MockAuthRepository(this._db);
  final MockDatabase _db;

  @override
  Future<AppUser> login(String identifier, String password) async {
    await fakeDelay(800);
    final id = identifier.trim().toLowerCase();
    final user = _db.users.where((u) => u.email.toLowerCase() == id || u.phone == id).firstOrNull;

    if (user == null || _db.passwords[user.id] != password) {
      throw const AppException('Incorrect email/phone or password.');
    }
    _db.currentUserId = user.id;
    return user;
  }

  @override
  Future<AppUser> register(RegisterRequest request) async {
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
    _db.currentUserId = user.id;

    // A corporate sign-up becomes a pending company link until the company approves it.
    if (request.accountType == AccountType.corporate && request.companyName != null) {
      _db.myAffiliations.add(CorporateAffiliation(
        id: _db.newId(),
        companyName: request.companyName!,
        role: CorporateRole.manager,
        status: AffiliationStatus.pending,
      ));
    }
    return user;
  }

  @override
  Future<void> logout() async {
    _db.currentUserId = null;
  }

  @override
  void restoreSession(AppUser user) {
    // The mock forgets registered users on restart, so add them back (with empty data).
    if (!_db.users.any((u) => u.id == user.id)) _db.users.add(user);
    _db.currentUserId = user.id;
  }
}
