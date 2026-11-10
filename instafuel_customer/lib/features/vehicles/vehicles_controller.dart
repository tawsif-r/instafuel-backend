import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/providers.dart';
import '../../models/vehicle.dart';
import '../auth/auth_controller.dart';

final vehiclesProvider = AsyncNotifierProvider<VehiclesController, List<Vehicle>>(VehiclesController.new);

/// One vehicle by id, taken from the loaded list (null if it doesn't exist).
final vehicleByIdProvider = Provider.family<AsyncValue<Vehicle?>, String>((ref, id) {
  return ref.watch(vehiclesProvider).whenData((vehicles) => vehicles.where((v) => v.id == id).firstOrNull);
});

class VehiclesController extends AsyncNotifier<List<Vehicle>> {
  @override
  Future<List<Vehicle>> build() async {
    // Watching auth reloads this list when the user logs in/out.
    if (ref.watch(authControllerProvider) == null) return [];
    return ref.read(vehicleRepositoryProvider).getVehicles();
  }

  Future<Vehicle> save(Vehicle vehicle) async {
    final saved = await ref.read(vehicleRepositoryProvider).saveVehicle(vehicle);
    ref.invalidateSelf();
    return saved;
  }

  Future<Vehicle> uploadDocument(String vehicleId, DocumentKind kind, List<int> bytes, String fileName) async {
    final saved = await ref.read(vehicleRepositoryProvider).uploadDocument(
          vehicleId: vehicleId,
          kind: kind,
          bytes: bytes,
          fileName: fileName,
        );
    ref.invalidateSelf();
    return saved;
  }

  Future<Vehicle> deleteDocument(String vehicleId, DocumentKind kind) async {
    final saved = await ref.read(vehicleRepositoryProvider).deleteDocument(vehicleId: vehicleId, kind: kind);
    ref.invalidateSelf();
    return saved;
  }

  Future<void> delete(String id) async {
    await ref.read(vehicleRepositoryProvider).deleteVehicle(id);
    ref.invalidateSelf();
  }
}
