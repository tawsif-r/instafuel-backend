import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import 'package:instafuel_customer/data/api/api_client.dart';
import 'package:instafuel_customer/data/api/api_vehicle_repository.dart';
import 'package:instafuel_customer/data/repositories.dart';
import 'package:instafuel_customer/models/fuel.dart';
import 'package:instafuel_customer/models/vehicle.dart';

// Shaped like a CustomerVehicle from GET /api/me/vehicles.
final _vehicleJson = {
  'id': '0b6f2c1e-0000-0000-0000-000000000001',
  'userId': '0b6f2c1e-0000-0000-0000-0000000000aa',
  'nickname': 'Generator',
  'vehicleType': 'generator',
  'registrationNumber': 'GEN-1',
  'fuelType': 'diesel',
  'verificationStatus': 'rejected',
  'rejectionReason': 'Plate photo blurry',
  'isActive': true,
  'documents': [
    {'id': 'd1', 'kind': 'registration_certificate', 'fileName': 'reg.pdf'},
    {'id': 'd2', 'kind': 'some_future_kind', 'fileName': 'x.pdf'},
  ],
};

ApiClient _client(Future<http.Response> Function(http.Request) handler) =>
    ApiClient(baseUrl: 'http://api.test', httpClient: MockClient(handler))..token = 'jwt';

void main() {
  test('getVehicles parses the API shape and skips unknown document kinds', () async {
    final api = _client((_) async => http.Response(jsonEncode([_vehicleJson]), 200));

    final vehicles = await ApiVehicleRepository(api).getVehicles();
    final v = vehicles.single;

    expect(v.type, VehicleType.generator);
    expect(v.fuelType, FuelType.diesel);
    expect(v.verificationStatus, VerificationStatus.rejected);
    expect(v.rejectionReason, 'Plate photo blurry');
    expect(v.documents.map((d) => d.kind), [DocumentKind.registrationCertificate]);
  });

  test('new vehicle is POSTed with snake_case document kinds; missing docs are left out', () async {
    late http.Request sent;
    final api = _client((request) async {
      sent = request;
      return http.Response(jsonEncode(_vehicleJson), 201);
    });

    await ApiVehicleRepository(api).saveVehicle(const Vehicle(
      id: '',
      nickname: 'Car',
      type: VehicleType.car,
      registrationNumber: 'DHAKA METRO-GA 12-3456',
      fuelType: FuelType.octane,
      documents: [
        VehicleDocument(kind: DocumentKind.numberPlatePhoto, fileName: 'plate.jpg'),
        VehicleDocument(kind: DocumentKind.fitnessCertificate), // not uploaded
      ],
    ));

    expect(sent.method, 'POST');
    expect(sent.url.path, '/api/me/vehicles');
    expect(sent.headers['Authorization'], 'Bearer jwt');
    expect(jsonDecode(sent.body), {
      'nickname': 'Car',
      'vehicleType': 'car',
      'registrationNumber': 'DHAKA METRO-GA 12-3456',
      'fuelType': 'octane',
      'documents': [
        {'kind': 'number_plate_photo', 'fileName': 'plate.jpg'},
      ],
    });
  });

  test('existing vehicle is PUT to its id', () async {
    late http.Request sent;
    final api = _client((request) async {
      sent = request;
      return http.Response(jsonEncode(_vehicleJson), 200);
    });

    await ApiVehicleRepository(api).saveVehicle(ApiVehicleRepository.vehicleFromJson(_vehicleJson));

    expect(sent.method, 'PUT');
    expect(sent.url.path, '/api/me/vehicles/${_vehicleJson['id']}');
  });

  test('duplicate plate (409) shows the API message', () async {
    final api = _client((_) async => http.Response('{"message":"This number plate is already registered."}', 409));

    expect(
      () => ApiVehicleRepository(api).saveVehicle(ApiVehicleRepository.vehicleFromJson(_vehicleJson)),
      throwsA(isA<AppException>().having((e) => e.message, 'message', 'This number plate is already registered.')),
    );
  });

  test('401 with a token triggers onUnauthorized', () async {
    var loggedOut = false;
    final api = _client((_) async => http.Response('', 401))..onUnauthorized = () => loggedOut = true;

    await expectLater(ApiVehicleRepository(api).getVehicles(), throwsA(isA<AppException>()));
    expect(loggedOut, isTrue);
  });
}
