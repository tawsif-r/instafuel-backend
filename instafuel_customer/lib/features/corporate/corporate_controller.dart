import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/providers.dart';
import '../../models/corporate.dart';
import '../auth/auth_controller.dart';

final affiliationsProvider =
    AsyncNotifierProvider<CorporateController, List<CorporateAffiliation>>(CorporateController.new);

class CorporateController extends AsyncNotifier<List<CorporateAffiliation>> {
  @override
  Future<List<CorporateAffiliation>> build() async {
    // Watching auth reloads this list when the user logs in/out.
    if (ref.watch(authControllerProvider) == null) return [];
    return ref.read(corporateRepositoryProvider).getAffiliations();
  }

  Future<CorporateAffiliation> join(String code) async {
    final affiliation = await ref.read(corporateRepositoryProvider).joinCompany(code);
    ref.invalidateSelf();
    return affiliation;
  }

  void refresh() => ref.invalidateSelf();
}
