import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:instafuel_customer/app.dart';
import 'package:instafuel_customer/data/providers.dart';

Future<void> _pumpApp(WidgetTester tester) async {
  SharedPreferences.setMockInitialValues({});
  final prefs = await SharedPreferences.getInstance();
  await tester.pumpWidget(ProviderScope(
    overrides: [sharedPreferencesProvider.overrideWithValue(prefs)],
    child: const InstafuelApp(),
  ));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('logged-out user lands on login', (tester) async {
    await _pumpApp(tester);
    expect(find.text('Welcome back'), findsOneWidget);
  });

  testWidgets('demo login opens the dashboard with the live tracker', (tester) async {
    await _pumpApp(tester);

    await tester.ensureVisible(find.text('Use'));
    await tester.tap(find.text('Use')); // fills demo credentials
    await tester.ensureVisible(find.widgetWithText(FilledButton, 'Log in'));
    await tester.tap(find.widgetWithText(FilledButton, 'Log in'));
    await tester.pumpAndSettle();

    expect(find.text('Hello, Ayesha'), findsOneWidget);
    expect(find.text('Tracking IF-1043'), findsOneWidget);
  });

  testWidgets('wrong password shows an error', (tester) async {
    await _pumpApp(tester);

    await tester.enterText(find.byType(TextFormField).first, 'demo@instafuel.app');
    await tester.enterText(find.byType(TextFormField).last, 'wrong-password');
    await tester.tap(find.widgetWithText(FilledButton, 'Log in'));
    await tester.pumpAndSettle();

    expect(find.text('Incorrect email/phone or password.'), findsOneWidget);
  });
}
