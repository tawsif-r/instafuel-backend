// Mock company links. Replace with ApiCorporateRepository once corporate
// accounts exist in the API.

import '../../models/corporate.dart';
import '../repositories.dart';
import 'mock_database.dart';

class MockCorporateRepository implements CorporateRepository {
  MockCorporateRepository(this._db);
  final MockDatabase _db;

  @override
  Future<List<CorporateAffiliation>> getAffiliations() async {
    await fakeDelay();
    return List.of(_db.myAffiliations);
  }

  @override
  Future<CorporateAffiliation> joinCompany(String companyCode) async {
    await fakeDelay(900);
    final code = companyCode.trim().toUpperCase();
    if (code.length < 4) throw const AppException('Company code looks too short.');

    // Demo: codes starting with "DEMO" are approved instantly with a credit limit.
    final instant = code.startsWith('DEMO');
    final affiliation = CorporateAffiliation(
      id: _db.newId(),
      companyName: 'Company $code',
      role: CorporateRole.member,
      status: instant ? AffiliationStatus.active : AffiliationStatus.pending,
      creditLimit: instant ? 25000 : 0,
    );
    _db.myAffiliations.add(affiliation);
    return affiliation;
  }
}
