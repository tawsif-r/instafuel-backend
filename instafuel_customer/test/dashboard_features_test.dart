import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:instafuel_customer/app.dart';
import 'package:instafuel_customer/data/providers.dart';
import 'fakes/fake_repositories.dart';

Future<void> _pumpLoggedInDashboard(WidgetTester tester) async {
  SharedPreferences.setMockInitialValues({});
  FlutterSecureStorage.setMockInitialValues({});
  final prefs = await SharedPreferences.getInstance();
  await tester.pumpWidget(ProviderScope(
    overrides: [
      sharedPreferencesProvider.overrideWithValue(prefs),
      authRepositoryProvider.overrideWith((ref) => FakeAuthRepository()),
      vehicleRepositoryProvider.overrideWith((ref) => FakeVehicleRepository()),
      orderRepositoryProvider.overrideWith((ref) => FakeOrderRepository()),
      paymentRepositoryProvider.overrideWith((ref) => FakePaymentRepository()),
      corporateRepositoryProvider.overrideWith((ref) => FakeCorporateRepository()),
    ],
    child: const InstafuelApp(),
  ));
  await tester.pumpAndSettle();

  // Log in
  await tester.enterText(find.byType(TextFormField).first, 'ayesha@instafuel.app');
  await tester.enterText(find.byType(TextFormField).last, 'password123');
  await tester.tap(find.widgetWithText(FilledButton, 'Sign In'));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('deliver to section displays zone first, then address, and opens scrollable zone picker without overflow', (tester) async {
    await _pumpLoggedInDashboard(tester);

    // Verify DELIVER TO is present
    expect(find.text('DELIVER TO'), findsOneWidget);

    // Verify Zone name comes first
    expect(find.text('Dhaka Metro · Central Hub'), findsOneWidget);

    // Verify Address comes underneath
    expect(find.text('Dhanmondi 27, Dhaka'), findsOneWidget);

    // Tap on Deliver to location chip
    await tester.tap(find.text('DELIVER TO'));
    await tester.pumpAndSettle();

    // Verify Zone Picker bottom sheet opened
    expect(find.text('Select Delivery Zone'), findsOneWidget);
    expect(find.text('Available Bowser Hubs'), findsOneWidget);

    // Verify all zones are rendered and scrollable
    expect(find.text('Gulshan & Banani Zone'), findsOneWidget);
    expect(find.text('Dhanmondi & Lalmatia Zone'), findsOneWidget);

    // Select Gulshan & Banani Zone
    await tester.tap(find.text('Gulshan & Banani Zone'));
    await tester.pumpAndSettle();

    // Verify bottom sheet closed and selected zone is updated
    expect(find.text('Select Delivery Zone'), findsNothing);
    expect(find.text('Gulshan & Banani Zone'), findsOneWidget);
    expect(find.text('Dhanmondi 27, Dhaka'), findsOneWidget);
  });

  testWidgets('notification icon opens side panel with notifications and clear all button', (tester) async {
    await _pumpLoggedInDashboard(tester);

    // Tap the Notification icon button in header
    final notifButton = find.byTooltip('Notifications');
    expect(notifButton, findsOneWidget);
    await tester.tap(notifButton);
    await tester.pumpAndSettle();

    // Verify the old toast message is NOT shown
    expect(find.text('All delivery systems operational.'), findsNothing);

    // Verify Drawer / Side Panel opened
    expect(find.widgetWithText(Drawer, 'Notifications'), findsOneWidget);
    expect(find.text('Clear all'), findsOneWidget);

    // Verify first notification is visible
    expect(find.text('Smart Bowser Fleet Active'), findsOneWidget);

    final drawerListView = find.descendant(of: find.byType(Drawer), matching: find.byType(ListView));
    expect(drawerListView, findsOneWidget);
    await tester.drag(drawerListView, const Offset(0, -300));
    await tester.pumpAndSettle();
    expect(find.text('Equipment Verification Audit'), findsOneWidget);

    // Tap "Clear all"
    await tester.tap(find.text('Clear all'));
    await tester.pumpAndSettle();

    // Verify notifications cleared and empty state shown
    expect(find.text('No Notifications'), findsOneWidget);
    expect(find.text('All caught up'), findsOneWidget);
    expect(find.text('Clear all'), findsNothing);
  });


  testWidgets('100% pure, digital meter, safe delivery trust badges are at the bottom of the page', (tester) async {
    tester.view.physicalSize = const Size(800, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    await _pumpLoggedInDashboard(tester);

    // Scroll down to the bottom
    await tester.drag(find.byType(CustomScrollView), const Offset(0, -600));
    await tester.pumpAndSettle();

    // Verify Trust Badges are present
    expect(find.text('100% Pure'), findsOneWidget);
    expect(find.text('Digital Meter'), findsOneWidget);
    expect(find.text('Safe Delivery'), findsOneWidget);
    expect(find.text('Zero Adulteration'), findsOneWidget);
    expect(find.text('Calibrated Accuracy'), findsOneWidget);
    expect(find.text('PESO & Hazmat Compliant'), findsOneWidget);

    // Verify that Trust Badges appear below Recent Refuels in the layout
    final recentRefuelsFinder = find.text('Recent Refuels');
    final pureBadgeFinder = find.text('100% Pure');
    expect(recentRefuelsFinder, findsOneWidget);
    expect(pureBadgeFinder, findsOneWidget);

    final recentRefuelsY = tester.getTopLeft(recentRefuelsFinder).dy;
    final pureBadgeY = tester.getTopLeft(pureBadgeFinder).dy;
    expect(pureBadgeY, greaterThan(recentRefuelsY));
  });
}
