// In-memory "backend" for the demo. Holds data per user so a freshly
// registered account starts empty, while the demo account has sample data.
//
// Demo login: demo@instafuel.app / password123

import '../../models/corporate.dart';
import '../../models/fuel.dart';
import '../../models/order.dart';
import '../../models/payment.dart';
import '../../models/user.dart';
import '../../models/vehicle.dart';

/// Simulates network latency so loading states are visible.
Future<void> fakeDelay([int ms = 600]) => Future.delayed(Duration(milliseconds: ms));

class MockDatabase {
  MockDatabase();

  final users = <AppUser>[];
  final passwords = <String, String>{}; // userId -> password
  final vehicles = <String, List<Vehicle>>{};
  final orders = <String, List<Order>>{};
  final affiliations = <String, List<CorporateAffiliation>>{};

  // Demo prices; the real ones come from centrally managed pricing.
  final products = const [
    FuelProduct(type: FuelType.octane, pricePerLitre: 135),
    FuelProduct(type: FuelType.petrol, pricePerLitre: 130),
    FuelProduct(type: FuelType.diesel, pricePerLitre: 114),
  ];

  // Flat demo fee. Real fee will depend on distance / delivery zone.
  static const deliveryFee = 150.0;

  String? currentUserId;
  int _nextId = 100;

  String newId() => '${_nextId++}';

  String get _uid {
    final id = currentUserId;
    if (id == null) throw StateError('No logged-in user');
    return id;
  }

  List<Vehicle> get myVehicles => vehicles.putIfAbsent(_uid, () => []);
  List<Order> get myOrders => orders.putIfAbsent(_uid, () => []);
  List<CorporateAffiliation> get myAffiliations => affiliations.putIfAbsent(_uid, () => []);

  double priceOf(FuelType type) => products.firstWhere((p) => p.type == type).pricePerLitre;

  factory MockDatabase.seeded() {
    final db = MockDatabase();
    final now = DateTime.now();

    const user = AppUser(
      id: 'u1',
      fullName: 'Ayesha Rahman',
      email: 'demo@instafuel.app',
      phone: '01700000000',
      accountType: AccountType.individual,
    );
    db.users.add(user);
    db.passwords[user.id] = 'password123';

    const allDocs = [
      VehicleDocument(kind: DocumentKind.registrationCertificate, fileName: 'registration.pdf'),
      VehicleDocument(kind: DocumentKind.fitnessCertificate, fileName: 'fitness.pdf'),
      VehicleDocument(kind: DocumentKind.numberPlatePhoto, fileName: 'plate.jpg'),
    ];

    const car = Vehicle(
      id: 'v1',
      nickname: 'Family car',
      type: VehicleType.car,
      registrationNumber: 'DHAKA METRO-GA 12-3456',
      fuelType: FuelType.octane,
      verificationStatus: VerificationStatus.verified,
      documents: allDocs,
    );
    const microbus = Vehicle(
      id: 'v2',
      nickname: 'Office microbus',
      type: VehicleType.microbus,
      registrationNumber: 'DHAKA METRO-CHA 51-0021',
      fuelType: FuelType.diesel,
      verificationStatus: VerificationStatus.verified,
      documents: allDocs,
    );
    const pickup = Vehicle(
      id: 'v3',
      nickname: 'Delivery pickup',
      type: VehicleType.pickup,
      registrationNumber: 'DHAKA METRO-NA 14-7788',
      fuelType: FuelType.diesel,
      documents: [
        VehicleDocument(kind: DocumentKind.registrationCertificate, fileName: 'registration.pdf'),
      ],
    );
    db.vehicles[user.id] = [car, microbus, pickup];

    OrderLine line(Vehicle v, double litres, {bool verified = false}) => OrderLine(
          vehicleId: v.id,
          vehicleName: v.nickname,
          registrationNumber: v.registrationNumber,
          fuelType: v.fuelType,
          litres: litres,
          unitPrice: db.priceOf(v.fuelType),
          plateVerified: verified,
        );

    db.orders[user.id] = [
      Order(
        id: 'o3',
        orderNumber: 'IF-1043',
        lines: [line(car, 20, verified: true), line(microbus, 40)],
        address: 'House 12, Road 5, Dhanmondi, Dhaka',
        scheduledAt: now.add(const Duration(minutes: 35)),
        createdAt: now.subtract(const Duration(days: 1, hours: 2)),
        deliveryFee: deliveryFee,
        status: OrderStatus.processing,
        deliveryStatus: DeliveryStatus.inTransit,
        paymentStatus: PaymentStatus.paid,
        paymentMethod: PaymentMethod.card,
        driverName: 'Rahim Uddin',
        truckRegistration: 'DHAKA METRO-TA 11-2233',
        timeline: [
          OrderEvent('Order placed', now.subtract(const Duration(days: 1, hours: 2))),
          OrderEvent('Payment received', now.subtract(const Duration(days: 1, hours: 2))),
          OrderEvent('Driver assigned', now.subtract(const Duration(hours: 2))),
          OrderEvent('Dispatched from depot', now.subtract(const Duration(minutes: 40))),
          OrderEvent('On the way', now.subtract(const Duration(minutes: 25))),
        ],
      ),
      Order(
        id: 'o2',
        orderNumber: 'IF-1042',
        lines: [line(microbus, 50)],
        address: 'Plot 7, Gulshan Avenue, Dhaka',
        scheduledAt: now.add(const Duration(days: 2, hours: 3)),
        createdAt: now.subtract(const Duration(hours: 5)),
        deliveryFee: deliveryFee,
        status: OrderStatus.confirmed,
        timeline: [OrderEvent('Order placed', now.subtract(const Duration(hours: 5)))],
      ),
      Order(
        id: 'o1',
        orderNumber: 'IF-1031',
        lines: [line(car, 25, verified: true)],
        address: 'House 12, Road 5, Dhanmondi, Dhaka',
        scheduledAt: now.subtract(const Duration(days: 6)),
        createdAt: now.subtract(const Duration(days: 8)),
        deliveryFee: deliveryFee,
        status: OrderStatus.completed,
        deliveryStatus: DeliveryStatus.delivered,
        paymentStatus: PaymentStatus.paid,
        paymentMethod: PaymentMethod.mobileBanking,
        driverName: 'Karim Hossain',
        truckRegistration: 'DHAKA METRO-TA 11-2240',
        timeline: [
          OrderEvent('Order placed', now.subtract(const Duration(days: 8))),
          OrderEvent('Payment received', now.subtract(const Duration(days: 8))),
          OrderEvent('Driver assigned', now.subtract(const Duration(days: 6, hours: 3))),
          OrderEvent('Dispatched from depot', now.subtract(const Duration(days: 6, hours: 1))),
          OrderEvent('Number plate verified', now.subtract(const Duration(days: 6, minutes: 5))),
          OrderEvent('Delivered 25 L', now.subtract(const Duration(days: 6))),
        ],
      ),
    ];

    db.affiliations[user.id] = const [
      CorporateAffiliation(
        id: 'c1',
        companyName: 'Northwind Logistics Ltd.',
        role: CorporateRole.member,
        status: AffiliationStatus.active,
        creditLimit: 50000,
        usedCredit: 12500,
      ),
    ];

    return db;
  }
}
