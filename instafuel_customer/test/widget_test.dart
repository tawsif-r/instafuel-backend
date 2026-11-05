import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:instafuel_customer/app.dart';
import 'package:instafuel_customer/data/mock/mock_auth_repository.dart';
import 'package:instafuel_customer/data/mock/mock_order_repository.dart';
import 'package:instafuel_customer/data/mock/mock_payment_repository.dart';
import 'package:instafuel_customer/data/mock/mock_vehicle_repository.dart';
import 'package:instafuel_customer/data/providers.dart';

// Widget tests run offline: auth, vehicles, orders and payments use the mock repositories instead of the API.
Future<void> _pumpApp(WidgetTester tester) async {
  SharedPreferences.setMockInitialValues({});
  FlutterSecureStorage.setMockInitialValues({});
  final prefs = await SharedPreferences.getInstance();
  await tester.pumpWidget(ProviderScope(
    overrides: [
      sharedPreferencesProvider.overrideWithValue(prefs),
      authRepositoryProvider.overrideWith((ref) => MockAuthRepository(ref.watch(mockDatabaseProvider))),
      vehicleRepositoryProvider.overrideWith((ref) => MockVehicleRepository(ref.watch(mockDatabaseProvider))),
      orderRepositoryProvider.overrideWith((ref) => MockOrderRepository(ref.watch(mockDatabaseProvider))),
      paymentRepositoryProvider.overrideWith((ref) => MockPaymentRepository(ref.watch(mockDatabaseProvider))),
    ],
    child: const InstafuelApp(),
  ));
  await tester.pumpAndSettle();
}

Future<void> _logIn(WidgetTester tester, String identifier, String password) async {
  await tester.enterText(find.byType(TextFormField).first, identifier);
  await tester.enterText(find.byType(TextFormField).last, password);
  await tester.tap(find.widgetWithText(FilledButton, 'Log in'));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('logged-out user lands on login', (tester) async {
    await _pumpApp(tester);
    expect(find.text('Welcome back'), findsOneWidget);
  });

  testWidgets('demo login opens the dashboard with the live tracker', (tester) async {
    await _pumpApp(tester);
    await _logIn(tester, 'demo@instafuel.app', 'password123');

    expect(find.text('Hello, Ayesha'), findsOneWidget);
    expect(find.text('Tracking IF-1043'), findsOneWidget);
  });

  testWidgets('wrong password shows an error', (tester) async {
    await _pumpApp(tester);
    await _logIn(tester, 'demo@instafuel.app', 'wrong-password');

    expect(find.text('Incorrect email/phone or password.'), findsOneWidget);
  });

  testWidgets('register creates the account and opens an empty dashboard', (tester) async {
    await _pumpApp(tester);
    await tester.tap(find.text('Sign up'));
    await tester.pumpAndSettle();

    Finder field(String label) => find.widgetWithText(TextFormField, label);
    await tester.enterText(field('Full name'), 'Karim Uddin');
    await tester.enterText(field('Email'), 'karim@example.com');
    await tester.enterText(field('Mobile number'), '01811111111');
    await tester.enterText(field('Password'), 'secret123');
    await tester.enterText(field('Confirm password'), 'secret123');
    await tester.ensureVisible(find.byType(CheckboxListTile));
    await tester.tap(find.byType(CheckboxListTile));
    await tester.ensureVisible(find.widgetWithText(FilledButton, 'Create account'));
    await tester.tap(find.widgetWithText(FilledButton, 'Create account'));
    await tester.pumpAndSettle();

    expect(find.text('Hello, Karim'), findsOneWidget);
    expect(find.textContaining('Tracking'), findsNothing); // new user has no orders
  });
}
