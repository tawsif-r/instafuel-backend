// A link between the customer and a company that pays for (some of) their
// orders using an approved credit limit.

enum AffiliationStatus {
  pending('Pending approval'),
  active('Active');

  const AffiliationStatus(this.label);
  final String label;
}

enum CorporateRole {
  member('Member'),
  manager('Manager');

  const CorporateRole(this.label);
  final String label;
}

class CorporateAffiliation {
  const CorporateAffiliation({
    required this.id,
    required this.companyName,
    required this.role,
    required this.status,
    this.creditLimit = 0,
    this.usedCredit = 0,
  });

  final String id;
  final String companyName;
  final CorporateRole role;
  final AffiliationStatus status;
  final double creditLimit;
  final double usedCredit;

  bool get isActive => status == AffiliationStatus.active;
  double get availableCredit => creditLimit - usedCredit;

  CorporateAffiliation copyWith({double? usedCredit, AffiliationStatus? status}) => CorporateAffiliation(
        id: id,
        companyName: companyName,
        role: role,
        status: status ?? this.status,
        creditLimit: creditLimit,
        usedCredit: usedCredit ?? this.usedCredit,
      );
}
