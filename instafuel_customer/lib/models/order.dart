import 'fuel.dart';
import 'payment.dart';

/// Business rule from docs/plan.md: orders must be placed at least 24h ahead.
/// Kept as a constant until it becomes a backend setting.
const orderLeadTime = Duration(hours: 24);

// Status names match the backend (Order.Status / Delivery.Status).

enum OrderStatus {
  pending('Pending'),
  confirmed('Confirmed'),
  processing('Processing'),
  completed('Completed'),
  cancelled('Cancelled'),
  failed('Failed');

  const OrderStatus(this.label);
  final String label;
}

enum DeliveryStatus {
  pending('Waiting for truck'),
  assigned('Driver assigned'),
  dispatched('Dispatched'),
  inTransit('On the way'),
  delivered('Delivered'),
  failed('Delivery failed'),
  cancelled('Cancelled');

  const DeliveryStatus(this.label);
  final String label;

  // The happy path, used for progress indicators.
  static const steps = [pending, assigned, dispatched, inTransit, delivered];
}

// One vehicle + fuel + quantity. An order can have many lines, so a single
// order can fuel several vehicles. Vehicle name/plate are copied in (snapshot),
// like the backend copies product names into OrderItem.
class OrderLine {
  const OrderLine({
    required this.vehicleId,
    required this.vehicleName,
    required this.registrationNumber,
    required this.fuelType,
    required this.litres,
    required this.unitPrice,
    this.plateVerified = false,
  });

  final String vehicleId;
  final String vehicleName;
  final String registrationNumber;
  final FuelType fuelType;
  final double litres;
  final double unitPrice;
  final bool plateVerified; // set by the driver app when the plate matches

  double get lineTotal => litres * unitPrice;
}

class OrderEvent {
  const OrderEvent(this.title, this.at);

  final String title;
  final DateTime at;
}

class Order {
  const Order({
    required this.id,
    required this.orderNumber,
    required this.lines,
    required this.address,
    required this.scheduledAt,
    required this.createdAt,
    required this.deliveryFee,
    this.status = OrderStatus.pending,
    this.deliveryStatus = DeliveryStatus.pending,
    this.paymentStatus = PaymentStatus.unpaid,
    this.paymentMethod,
    this.corporateId,
    this.notes,
    this.driverName,
    this.truckRegistration,
    this.timeline = const [],
  });

  final String id;
  final String orderNumber;
  final List<OrderLine> lines;
  final String address;
  final DateTime scheduledAt;
  final DateTime createdAt;
  final double deliveryFee;
  final OrderStatus status;
  final DeliveryStatus deliveryStatus;
  final PaymentStatus paymentStatus;
  final PaymentMethod? paymentMethod;
  final String? corporateId; // billed to this company when set
  final String? notes;
  final String? driverName;        // filled once a driver is assigned
  final String? truckRegistration; // filled once a truck is assigned
  final List<OrderEvent> timeline;

  double get subtotal => lines.fold(0, (sum, l) => sum + l.lineTotal);
  double get total => subtotal + deliveryFee;
  double get totalLitres => lines.fold(0, (sum, l) => sum + l.litres);

  bool get isActive =>
      status != OrderStatus.completed && status != OrderStatus.cancelled && status != OrderStatus.failed;

  bool get canCancel =>
      (status == OrderStatus.pending || status == OrderStatus.confirmed) &&
      deliveryStatus == DeliveryStatus.pending;

  bool get needsPayment => isActive && (paymentStatus == PaymentStatus.unpaid || paymentStatus == PaymentStatus.failed);

  Order copyWith({
    OrderStatus? status,
    DeliveryStatus? deliveryStatus,
    PaymentStatus? paymentStatus,
    PaymentMethod? paymentMethod,
    List<OrderEvent>? timeline,
  }) =>
      Order(
        id: id,
        orderNumber: orderNumber,
        lines: lines,
        address: address,
        scheduledAt: scheduledAt,
        createdAt: createdAt,
        deliveryFee: deliveryFee,
        status: status ?? this.status,
        deliveryStatus: deliveryStatus ?? this.deliveryStatus,
        paymentStatus: paymentStatus ?? this.paymentStatus,
        paymentMethod: paymentMethod ?? this.paymentMethod,
        corporateId: corporateId,
        notes: notes,
        driverName: driverName,
        truckRegistration: truckRegistration,
        timeline: timeline ?? this.timeline,
      );
}

// What the "New order" screen builds up before submitting.
class OrderDraft {
  const OrderDraft({
    required this.lines,
    required this.address,
    required this.scheduledAt,
    this.notes,
    this.corporateId,
  });

  final List<OrderLineDraft> lines;
  final String address;
  final DateTime scheduledAt;
  final String? notes;
  final String? corporateId;
}

class OrderLineDraft {
  const OrderLineDraft({required this.vehicleId, required this.fuelType, required this.litres});

  final String vehicleId;
  final FuelType fuelType;
  final double litres;
}

// Price preview shown before the customer confirms. Calculated by the
// repository (backend later) so pricing rules stay in one place.
class OrderQuote {
  const OrderQuote({required this.subtotal, required this.deliveryFee});

  final double subtotal;
  final double deliveryFee;
  double get total => subtotal + deliveryFee;
}
