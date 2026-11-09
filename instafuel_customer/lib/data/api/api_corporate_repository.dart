// Customer corporate links against Instafuel.Api (/api/me/corporate).
// Scoped to the logged-in customer's token.

import '../../models/corporate.dart';
import '../repositories.dart';
import 'api_client.dart';

class ApiCorporateRepository implements CorporateRepository {
  ApiCorporateRepository(this._api);
  final ApiClient _api;

  static const _path = '/api/me/corporate';

  @override
  Future<List<CorporateAffiliation>> getAffiliations() async {
    final json = await _api.get(_path) as List<dynamic>;
    return json.map((a) => affiliationFromJson(a as Map<String, dynamic>)).toList();
  }

  @override
  Future<CorporateAffiliation> joinCompany(String companyCode) async {
    final json = await _api.post('$_path/join', {'companyCode': companyCode.trim()});
    return affiliationFromJson(json as Map<String, dynamic>);
  }

  // Public for tests.
  static CorporateAffiliation affiliationFromJson(Map<String, dynamic> json) => CorporateAffiliation(
        id: json['id']?.toString() ?? '',
        companyName: (json['companyName'] as String?) ?? 'Unknown Company',
        role: json['role'] == 'manager' ? CorporateRole.manager : CorporateRole.member,
        status: json['status'] == 'active' ? AffiliationStatus.active : AffiliationStatus.pending,
        creditLimit: (json['creditLimit'] as num?)?.toDouble() ?? 0.0,
        usedCredit: (json['usedCredit'] as num?)?.toDouble() ?? 0.0,
      );
}

