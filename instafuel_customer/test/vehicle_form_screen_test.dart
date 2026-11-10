import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:instafuel_customer/features/vehicles/vehicle_form_screen.dart';
import 'package:instafuel_customer/features/vehicles/vehicles_controller.dart';
import 'package:instafuel_customer/models/fuel.dart';
import 'package:instafuel_customer/models/vehicle.dart';

void main() {
  const testVehicle = Vehicle(
    id: 'v1',
    nickname: 'Toyota Prado',
    type: VehicleType.car,
    registrationNumber: 'DHAKA METRO-GA 11-2233',
    fuelType: FuelType.octane,
    verificationStatus: VerificationStatus.verified,
  );

  testWidgets('renders all form fields, document cards, and actions for existing vehicle', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          vehiclesProvider.overrideWith(() => FakeVehiclesController([testVehicle])),
        ],
        child: const MaterialApp(
          home: VehicleFormScreen(vehicleId: 'v1'),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Verify AppBar
    expect(find.widgetWithText(AppBar, 'Equipment Details'), findsOneWidget);

    // Verify Nickname field
    expect(find.text('Equipment Nickname'), findsOneWidget);
    expect(find.text('Toyota Prado'), findsWidgets);

    // Verify Plate field
    expect(find.text('Registration / Plate Number (BRTA)'), findsOneWidget);

    // Verify Compliance & Verification section
    expect(find.text('Compliance & Verification'), findsOneWidget);

    // Scroll down to make all document cards and buttons visible
    await tester.drag(find.byType(ListView), const Offset(0, -300));
    await tester.pumpAndSettle();

    // Verify all 3 document cards
    expect(find.text('Registration certificate'), findsOneWidget);
    expect(find.text('Fitness certificate'), findsOneWidget);
    expect(find.text('Number plate photo'), findsOneWidget);

    // Verify bottom navigation bar buttons
    expect(find.text('Save Changes'), findsOneWidget);
    expect(find.text('Refuel Now'), findsOneWidget);
  });

  testWidgets('renders clean empty form for new equipment registration', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          vehiclesProvider.overrideWith(() => FakeVehiclesController([])),
        ],
        child: const MaterialApp(
          home: VehicleFormScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Verify AppBar
    expect(find.text('Register Vehicle or Generator'), findsOneWidget);

    // Verify fields
    expect(find.text('Equipment Nickname'), findsOneWidget);
    expect(find.text('Registration / Plate Number (BRTA)'), findsOneWidget);

    // Verify Register button
    expect(find.text('Register Equipment (SOP §4.3)'), findsOneWidget);
  });

  testWidgets('tapping upload opens bottom sheet with test attachment options', (tester) async {
    tester.view.physicalSize = const Size(800, 1200);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          vehiclesProvider.overrideWith(() => FakeVehiclesController([testVehicle])),
        ],
        child: const MaterialApp(
          home: VehicleFormScreen(vehicleId: 'v1'),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Find the first upload button and tap it
    final uploadButtons = find.text('Upload');
    expect(uploadButtons, findsWidgets);

    await tester.tap(uploadButtons.first);
    await tester.pumpAndSettle();

    // Verify upload modal is displayed
    expect(find.text('Testing Mode: Select any real PDF or image from your storage, or attach a mock document.'), findsOneWidget);
    expect(find.text('Browse Device (Any PDF or Image)'), findsOneWidget);

    // Tap the test asset button to attach
    final testAssetFinder = find.byIcon(Icons.picture_as_pdf_rounded);
    expect(testAssetFinder, findsWidgets);

    await tester.tap(testAssetFinder.first);
    await tester.pumpAndSettle();

    // Upload count should update to 1/3
    expect(find.text('1/3 Uploaded'), findsOneWidget);
  });


}

class FakeVehiclesController extends VehiclesController {
  FakeVehiclesController(this.initial);
  final List<Vehicle> initial;

  @override
  Future<List<Vehicle>> build() async => initial;

  @override
  Future<Vehicle> save(Vehicle vehicle) async => vehicle;
}
