import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:instafuel_customer/app.dart';
import 'package:instafuel_customer/data/providers.dart';
import 'package:instafuel_customer/models/user.dart';
import 'fakes/fake_repositories.dart';

class ConfigurableFakeAuthRepository extends FakeAuthRepository {
  ConfigurableFakeAuthRepository({required this.user});

  final AppUser user;

  @override
  Future<AuthSession> login(String identifier, String password) async {
    loggedInUser = user;
    return AuthSession(
      token: 'fake-jwt-token',
      user: user,
      expiresAt: DateTime.now().add(const Duration(days: 7)),
    );
  }
}

void main() {
  testWidgets('corporate user does NOT see add company option in My Account', (tester) async {
    SharedPreferences.setMockInitialValues({});
    FlutterSecureStorage.setMockInitialValues({});
    final prefs = await SharedPreferences.getInstance();

    final fakeAuth = ConfigurableFakeAuthRepository(
      user: const AppUser(
        id: 'u-corp',
        fullName: 'Rahim Ahmed',
        email: 'rahim@corporate.com',
        phone: '01700000000',
        accountType: AccountType.corporate,
        companyName: 'Apex Logistics Ltd',
      ),
    );

    await tester.pumpWidget(ProviderScope(
      overrides: [
        sharedPreferencesProvider.overrideWithValue(prefs),
        authRepositoryProvider.overrideWithValue(fakeAuth),
        vehicleRepositoryProvider.overrideWith((ref) => FakeVehicleRepository()),
        orderRepositoryProvider.overrideWith((ref) => FakeOrderRepository()),
        paymentRepositoryProvider.overrideWith((ref) => FakePaymentRepository()),
        corporateRepositoryProvider.overrideWith((ref) => FakeCorporateRepository()),
      ],
      child: const InstafuelApp(),
    ));
    await tester.pumpAndSettle();

    // Log in
    await tester.enterText(find.byType(TextFormField).first, 'rahim@corporate.com');
    await tester.enterText(find.byType(TextFormField).last, 'password123');
    await tester.tap(find.widgetWithText(FilledButton, 'Sign In'));
    await tester.pumpAndSettle();

    // Navigate to Account tab (bottom bar destination index 3)
    final accountTab = find.text('Account');
    expect(accountTab, findsOneWidget);
    await tester.tap(accountTab);
    await tester.pumpAndSettle();

    // Verify company name is shown in profile
    expect(find.text('Apex Logistics Ltd'), findsOneWidget);
    expect(find.text('CORPORATE'), findsOneWidget);

    // Verify "Corporate Accounts & Credit Limit" (the add/link company option) is REMOVED
    expect(find.text('Corporate Accounts & Credit Limit'), findsNothing);
    expect(find.text('Link company for 30-day billing (SOP §12.3)'), findsNothing);

    // Verify Garage & Equipment is still accessible
    expect(find.text('My Fuel Garage & Equipment'), findsOneWidget);
  });

  testWidgets('individual user DOES see corporate link option in My Account', (tester) async {
    SharedPreferences.setMockInitialValues({});
    FlutterSecureStorage.setMockInitialValues({});
    final prefs = await SharedPreferences.getInstance();

    final fakeAuth = ConfigurableFakeAuthRepository(
      user: const AppUser(
        id: 'u-personal',
        fullName: 'Karim Uddin',
        email: 'karim@personal.com',
        phone: '01800000000',
        accountType: AccountType.individual,
      ),
    );

    await tester.pumpWidget(ProviderScope(
      overrides: [
        sharedPreferencesProvider.overrideWithValue(prefs),
        authRepositoryProvider.overrideWithValue(fakeAuth),
        vehicleRepositoryProvider.overrideWith((ref) => FakeVehicleRepository()),
        orderRepositoryProvider.overrideWith((ref) => FakeOrderRepository()),
        paymentRepositoryProvider.overrideWith((ref) => FakePaymentRepository()),
        corporateRepositoryProvider.overrideWith((ref) => FakeCorporateRepository()),
      ],
      child: const InstafuelApp(),
    ));
    await tester.pumpAndSettle();

    // Log in
    await tester.enterText(find.byType(TextFormField).first, 'karim@personal.com');
    await tester.enterText(find.byType(TextFormField).last, 'password123');
    await tester.tap(find.widgetWithText(FilledButton, 'Sign In'));
    await tester.pumpAndSettle();

    // Navigate to Account tab
    final accountTab = find.text('Account');
    expect(accountTab, findsOneWidget);
    await tester.tap(accountTab);
    await tester.pumpAndSettle();

    // Verify "Corporate Accounts & Credit Limit" IS shown for individual users
    expect(find.text('Corporate Accounts & Credit Limit'), findsOneWidget);
  });
}

