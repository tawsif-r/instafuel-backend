import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import 'package:instafuel_customer/data/api/api_auth_repository.dart';
import 'package:instafuel_customer/data/api/api_client.dart';
import 'package:instafuel_customer/data/repositories.dart';
import 'package:instafuel_customer/models/user.dart';

// Fake API responses shaped like Instafuel.Api's AuthController.
final _authJson = {
  'token': 'jwt-123',
  'expiresAt': '2030-01-01T00:00:00Z',
  'user': {
    'id': '01a1a5da-e726-7b8f-835d-e561111a42c3',
    'fullName': 'Test Customer',
    'email': 'test@example.com',
    'phone': '01711111111',
    'role': 'customer',
    'accountType': 'corporate',
    'isActive': true,
  },
};

void main() {
  test('login sends credentials, parses the session and keeps the token', () async {
    late http.Request sent;
    final api = ApiClient(
      baseUrl: 'http://api.test',
      httpClient: MockClient((request) async {
        sent = request;
        return http.Response(jsonEncode(_authJson), 200);
      }),
    );

    final session = await ApiAuthRepository(api).login(' test@example.com ', 'secret123');

    expect(sent.url.toString(), 'http://api.test/api/auth/login');
    expect(jsonDecode(sent.body), {'identifier': 'test@example.com', 'password': 'secret123'});
    expect(session.user.accountType, AccountType.corporate);
    expect(session.isExpired, isFalse);
    expect(api.token, 'jwt-123');
  });

  test('API error message is shown to the user', () async {
    final api = ApiClient(
      baseUrl: 'http://api.test',
      httpClient: MockClient((_) async => http.Response('{"message":"Incorrect email/phone or password."}', 401)),
    );

    expect(
      () => ApiAuthRepository(api).login('a@b.com', 'nope'),
      throwsA(isA<AppException>().having((e) => e.message, 'message', 'Incorrect email/phone or password.')),
    );
  });

  test('validation errors use the first field message', () {
    final message = ApiClient.errorMessage(400, {
      'errors': {
        'Password': ['Password must be at least 8 characters.'],
      },
    });
    expect(message, 'Password must be at least 8 characters.');
  });

  test('network failure becomes a friendly error', () async {
    final api = ApiClient(
      baseUrl: 'http://api.test',
      httpClient: MockClient((_) async => throw http.ClientException('Connection refused')),
    );

    expect(
      () => api.post('/api/auth/login', {}),
      throwsA(isA<AppException>().having((e) => e.message, 'message', contains("Can't reach the server"))),
    );
  });

  test('saved session round-trips through JSON', () {
    final session = AuthSession.fromJson(_authJson);
    final restored = AuthSession.fromJson(jsonDecode(jsonEncode(session.toJson())) as Map<String, dynamic>);

    expect(restored.token, 'jwt-123');
    expect(restored.user.email, 'test@example.com');
    expect(restored.expiresAt, DateTime.utc(2030));
  });
}
