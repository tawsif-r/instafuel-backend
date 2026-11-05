enum AccountType {
  individual('Individual'),
  corporate('Corporate');

  const AccountType(this.label);
  final String label;
}

class AppUser {
  const AppUser({
    required this.id,
    required this.fullName,
    required this.email,
    required this.phone,
    required this.accountType,
    this.companyName,
  });

  final String id;
  final String fullName;
  final String email;
  final String phone;
  final AccountType accountType;
  final String? companyName; // only for corporate accounts

  String get firstName => fullName.split(' ').first;

  String get initials {
    final parts = fullName.trim().split(RegExp(r'\s+'));
    return parts.take(2).map((p) => p[0].toUpperCase()).join();
  }

  // Same shape as the API's user JSON (extra API fields are ignored).
  Map<String, dynamic> toJson() => {
        'id': id,
        'fullName': fullName,
        'email': email,
        'phone': phone,
        'accountType': accountType.name,
        'companyName': companyName,
      };

  factory AppUser.fromJson(Map<String, dynamic> json) => AppUser(
        id: json['id'] as String,
        fullName: json['fullName'] as String,
        email: json['email'] as String? ?? '',
        phone: json['phone'] as String? ?? '',
        accountType: AccountType.values.byName(json['accountType'] as String),
        companyName: json['companyName'] as String?,
      );
}

/// What login/register return: the user plus the token for later API calls.
class AuthSession {
  const AuthSession({required this.token, required this.expiresAt, required this.user});

  final String token; // JWT, sent as "Authorization: Bearer <token>"
  final DateTime expiresAt;
  final AppUser user;

  bool get isExpired => DateTime.now().isAfter(expiresAt);

  Map<String, dynamic> toJson() => {
        'token': token,
        'expiresAt': expiresAt.toUtc().toIso8601String(),
        'user': user.toJson(),
      };

  // Reads both the API response and what SessionStore saved.
  factory AuthSession.fromJson(Map<String, dynamic> json) => AuthSession(
        token: json['token'] as String,
        expiresAt: DateTime.parse(json['expiresAt'] as String),
        user: AppUser.fromJson(json['user'] as Map<String, dynamic>),
      );
}

// What the register form sends to AuthRepository.register.
class RegisterRequest {
  const RegisterRequest({
    required this.fullName,
    required this.email,
    required this.phone,
    required this.password,
    required this.accountType,
    this.companyName,
  });

  final String fullName;
  final String email;
  final String phone;
  final String password;
  final AccountType accountType;
  final String? companyName;
}
