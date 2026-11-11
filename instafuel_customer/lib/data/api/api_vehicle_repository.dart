// Customer vehicles against Instafuel.Api (/api/me/vehicles). The API only
// returns the logged-in user's vehicles, based on the token.
//
// The API uses snake_case strings ("registration_certificate"); the app uses
// enums (DocumentKind.registrationCertificate). The maps below convert both ways.

import 'dart:typed_data';

import '../../models/fuel.dart';
import '../../models/vehicle.dart';
import '../repositories.dart';
import 'api_client.dart';

const _documentKinds = {
  DocumentKind.registrationCertificate: 'registration_certificate',
  DocumentKind.fitnessCertificate: 'fitness_certificate',
  DocumentKind.numberPlatePhoto: 'number_plate_photo',
};

class ApiVehicleRepository implements VehicleRepository {
  ApiVehicleRepository(this._api);
  final ApiClient _api;

  static const _path = '/api/me/vehicles';

  @override
  Future<List<Vehicle>> getVehicles() async {
    final json = await _api.get(_path) as List<dynamic>;
    return json.map((v) => vehicleFromJson(v as Map<String, dynamic>)).toList();
  }

  /// Creates the vehicle when its id is empty, otherwise updates it.
  @override
  Future<Vehicle> saveVehicle(Vehicle vehicle) async {
    final body = vehicleToJson(vehicle);
    final json = vehicle.id.isEmpty ? await _api.post(_path, body) : await _api.put('$_path/${vehicle.id}', body);
    return vehicleFromJson(json as Map<String, dynamic>);
  }

  @override
  Future<void> deleteVehicle(String id) => _api.delete('$_path/$id');

  @override
  Future<Vehicle> uploadDocument({
    required String vehicleId,
    required DocumentKind kind,
    required List<int> bytes,
    required String fileName,
  }) async {
    final json = await _api.upload('$_path/$vehicleId/documents/${_documentKinds[kind]}', bytes: bytes, fileName: fileName);
    return vehicleFromJson(json as Map<String, dynamic>);
  }

  @override
  Future<Vehicle> deleteDocument({required String vehicleId, required DocumentKind kind}) async {
    final json = await _api.delete('$_path/$vehicleId/documents/${_documentKinds[kind]}');
    return vehicleFromJson(json as Map<String, dynamic>);
  }

  @override
  Future<Uint8List> downloadDocument({required String vehicleId, required DocumentKind kind}) =>
      _api.getBytes('$_path/$vehicleId/documents/${_documentKinds[kind]}/file');

  // Public for tests.
  static Map<String, dynamic> vehicleToJson(Vehicle v) => {
        'nickname': v.nickname,
        'vehicleType': v.type.name, // enum names match the API values
        'registrationNumber': v.registrationNumber,
        'fuelType': v.fuelType.name,
        // Real files go through uploadDocument(). Leaving "documents" out tells the
        // API to keep the vehicle's documents unchanged. Only name-only documents
        // (no stored file) are still sent this way.
        if (v.documents.any((d) => d.isUploaded && !d.hasFile))
          'documents': [
            for (final d in v.documents)
              if (d.isUploaded) {'kind': _documentKinds[d.kind], 'fileName': d.fileName},
          ],
      };

  static Vehicle vehicleFromJson(Map<String, dynamic> json) => Vehicle(
        id: json['id'] as String,
        nickname: json['nickname'] as String,
        type: VehicleType.values.byName(json['vehicleType'] as String),
        registrationNumber: json['registrationNumber'] as String,
        fuelType: FuelType.values.byName(json['fuelType'] as String),
        verificationStatus: VerificationStatus.values.byName(json['verificationStatus'] as String),
        rejectionReason: json['rejectionReason'] as String?,
        createdAt: json['createdAt'] == null ? null : DateTime.parse(json['createdAt'] as String).toLocal(),
        documents: [
          for (final d in json['documents'] as List<dynamic>)
            if (_kindFromApi(d['kind'] as String) case final kind?)
              VehicleDocument(
                kind: kind,
                fileName: d['fileName'] as String,
                hasFile: d['hasFile'] as bool? ?? false,
                contentType: d['contentType'] as String?,
                sizeBytes: (d['sizeBytes'] as num?)?.toInt(),
                uploadedAt: d['uploadedAt'] == null ? null : DateTime.parse(d['uploadedAt'] as String).toLocal(),
              ),
        ],
      );

  // Unknown kinds (added to the API later) are skipped instead of crashing.
  static DocumentKind? _kindFromApi(String value) =>
      _documentKinds.entries.where((e) => e.value == value).map((e) => e.key).firstOrNull;
}
