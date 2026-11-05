import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/providers.dart';
import '../../models/vehicle.dart';
import '../auth/auth_controller.dart';

final vehiclesProvider = AsyncNotifierProvider<VehiclesController, List<Vehicle>>(VehiclesController.new);

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

  Future<void> delete(String id) async {
    await ref.read(vehicleRepositoryProvider).deleteVehicle(id);
    ref.invalidateSelf();
  }
}
