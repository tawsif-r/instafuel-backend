import 'package:flutter/material.dart';

import 'fuel.dart';

// A customer's own vehicle. Orders are only accepted for verified vehicles,
// and the driver matches the number plate before fuelling.

enum VehicleType {
  car('Car', Icons.directions_car_outlined),
  motorcycle('Motorcycle', Icons.two_wheeler_outlined),
  microbus('Microbus', Icons.airport_shuttle_outlined),
  pickup('Pickup', Icons.local_shipping_outlined),
  truck('Truck', Icons.fire_truck_outlined),
  generator('Generator', Icons.bolt_outlined);

  const VehicleType(this.label, this.icon);
  final String label;
  final IconData icon;
}

enum VerificationStatus {
  pending('Verification pending'),
  verified('Verified'),
  rejected('Rejected');

  const VerificationStatus(this.label);
  final String label;
}

enum DocumentKind {
  registrationCertificate('Registration certificate', 'PDF of the registration paper', false),
  fitnessCertificate('Fitness certificate', 'PDF of the fitness certificate', false),
  numberPlatePhoto('Number plate photo', 'Clear photo of the number plate', true);

  const DocumentKind(this.label, this.hint, this.isPhoto);
  final String label;
  final String hint;
  final bool isPhoto; // photo vs PDF, decides the icon and (later) the picker
}

class VehicleDocument {
  const VehicleDocument({required this.kind, this.fileName});

  final DocumentKind kind;
  final String? fileName; // null = not uploaded yet. Later: a storage URL/id.

  bool get isUploaded => fileName != null;
}

class Vehicle {
  const Vehicle({
    required this.id,
    required this.nickname,
    required this.type,
    required this.registrationNumber,
    required this.fuelType,
    this.verificationStatus = VerificationStatus.pending,
    this.rejectionReason,
    this.documents = const [],
  });

  final String id;
  final String nickname;
  final VehicleType type;
  final String registrationNumber;
  final FuelType fuelType;
  final VerificationStatus verificationStatus;
  final String? rejectionReason; // set by the reviewer when rejected
  final List<VehicleDocument> documents;

  bool get isVerified => verificationStatus == VerificationStatus.verified;

  VehicleDocument document(DocumentKind kind) =>
      documents.firstWhere((d) => d.kind == kind, orElse: () => VehicleDocument(kind: kind));

  int get uploadedCount => DocumentKind.values.where((k) => document(k).isUploaded).length;

  bool get documentsComplete => uploadedCount == DocumentKind.values.length;

  Vehicle copyWith({
    String? nickname,
    VehicleType? type,
    String? registrationNumber,
    FuelType? fuelType,
    VerificationStatus? verificationStatus,
    List<VehicleDocument>? documents,
  }) =>
      Vehicle(
        id: id,
        nickname: nickname ?? this.nickname,
        type: type ?? this.type,
        registrationNumber: registrationNumber ?? this.registrationNumber,
        fuelType: fuelType ?? this.fuelType,
        verificationStatus: verificationStatus ?? this.verificationStatus,
        rejectionReason: rejectionReason,
        documents: documents ?? this.documents,
      );
}
