// Real auth against Instafuel.Api (/api/auth/register, /api/auth/login).
// The API returns { token, expiresAt, user } for both.

import '../../models/user.dart';
import '../repositories.dart';
import 'api_client.dart';

class ApiAuthRepository implements AuthRepository {
  ApiAuthRepository(this._api);
  final ApiClient _api;

  @override
  Future<AuthSession> login(String identifier, String password) async {
    final json = await _api.post('/api/auth/login', {
      'identifier': identifier.trim(),
      'password': password,
    });
    return _start(json);
  }

  @override
  Future<AuthSession> register(RegisterRequest request) async {
    final json = await _api.post('/api/auth/register', {
      'fullName': request.fullName.trim(),
      'email': request.email.trim(),
      'phone': request.phone.trim(),
      'password': request.password,
      'accountType': request.accountType.name,
    });
    return _start(json);
  }

  // JWTs are stateless, so logging out just means forgetting the token.
  @override
  Future<void> logout() async => _api.token = null;

  @override
  void restoreSession(AuthSession session) => _api.token = session.token;

  AuthSession _start(dynamic json) {
    final session = AuthSession.fromJson(json as Map<String, dynamic>);
    _api.token = session.token;
    return session;
  }
}
