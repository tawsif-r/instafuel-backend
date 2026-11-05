// Offline vehicles for tests and demos (MOCK_API=true). The app normally uses
// ApiVehicleRepository.

import '../../models/vehicle.dart';
import '../repositories.dart';
import 'mock_database.dart';

class MockVehicleRepository implements VehicleRepository {
  MockVehicleRepository(this._db);
  final MockDatabase _db;

  @override
  Future<List<Vehicle>> getVehicles() async {
    await fakeDelay();
    return List.of(_db.myVehicles);
  }

  @override
  Future<Vehicle> saveVehicle(Vehicle vehicle) async {
    await fakeDelay(800);
    final plate = vehicle.registrationNumber.trim().toUpperCase();
    final duplicate = _db.myVehicles.any((v) => v.registrationNumber == plate && v.id != vehicle.id);
    if (duplicate) throw const AppException('You already registered a vehicle with this number plate.');

    // Demo rule: all documents attached -> verified instantly.
    // In the real system, an admin reviews the documents.
    final saved = vehicle.copyWith(
      registrationNumber: plate,
      verificationStatus: vehicle.documentsComplete ? VerificationStatus.verified : VerificationStatus.pending,
    );

    if (vehicle.id.isEmpty) {
      final created = Vehicle(
        id: _db.newId(),
        nickname: saved.nickname,
        type: saved.type,
        registrationNumber: saved.registrationNumber,
        fuelType: saved.fuelType,
        verificationStatus: saved.verificationStatus,
        documents: saved.documents,
      );
      _db.myVehicles.add(created);
      return created;
    }

    final index = _db.myVehicles.indexWhere((v) => v.id == vehicle.id);
    if (index == -1) throw const AppException('Vehicle not found.');
    _db.myVehicles[index] = saved;
    return saved;
  }

  @override
  Future<void> deleteVehicle(String id) async {
    await fakeDelay();
    final inUse = _db.myOrders.any((o) => o.isActive && o.lines.any((l) => l.vehicleId == id));
    if (inUse) throw const AppException('This vehicle is part of an active order.');
    _db.myVehicles.removeWhere((v) => v.id == id);
  }
}
