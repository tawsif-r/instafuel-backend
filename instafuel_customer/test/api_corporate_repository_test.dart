import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import 'package:instafuel_customer/data/api/api_client.dart';
import 'package:instafuel_customer/data/api/api_corporate_repository.dart';
import 'package:instafuel_customer/models/corporate.dart';

final _affiliationJson = {
  'id': '0b6f2c1e-0000-0000-0000-000000000001',
  'companyName': 'Northwind Logistics Ltd.',
  'role': 'member',
  'status': 'active',
  'creditLimit': 50000.0,
  'usedCredit': 12500.0,
};

ApiClient _client(Future<http.Response> Function(http.Request) handler) =>
    ApiClient(baseUrl: 'http://api.test', httpClient: MockClient(handler))..token = 'jwt';

void main() {
  test('getAffiliations parses corporate list correctly', () async {
    final api = _client((_) async => http.Response(jsonEncode([_affiliationJson]), 200));

    final affiliations = await ApiCorporateRepository(api).getAffiliations();
    final a = affiliations.single;

    expect(a.id, '0b6f2c1e-0000-0000-0000-000000000001');
    expect(a.companyName, 'Northwind Logistics Ltd.');
    expect(a.role, CorporateRole.member);
    expect(a.status, AffiliationStatus.active);
    expect(a.creditLimit, 50000.0);
    expect(a.usedCredit, 12500.0);
    expect(a.availableCredit, 37500.0);
  });

  test('joinCompany sends companyCode and parses response', () async {
    late http.Request sent;
    final api = _client((request) async {
      sent = request;
      return http.Response(jsonEncode(_affiliationJson), 200);
    });

    final affiliation = await ApiCorporateRepository(api).joinCompany('DEMO123');

    expect(sent.method, 'POST');
    expect(sent.url.path, '/api/me/corporate/join');
    expect(jsonDecode(sent.body), {'companyCode': 'DEMO123'});
    expect(affiliation.companyName, 'Northwind Logistics Ltd.');
  });
}

