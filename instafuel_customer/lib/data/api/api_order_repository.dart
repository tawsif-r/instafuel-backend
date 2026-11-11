// Customer orders against Instafuel.Api (/api/me/orders). The API only returns
// the logged-in user's orders, based on the token.
//
// The API uses snake_case strings ("in_transit", "mobile_banking"); the app uses
// enums. The maps below convert both ways and are shared with ApiPaymentRepository.

import '../../models/fuel.dart';
import '../../models/order.dart';
import '../../models/payment.dart';
import '../repositories.dart';
import 'api_client.dart';

const paymentMethodValues = {
  PaymentMethod.cashOnDelivery: 'cash',
  PaymentMethod.card: 'card',
  PaymentMethod.mobileBanking: 'mobile_banking',
  PaymentMethod.bankTransfer: 'bank_transfer',
  PaymentMethod.corporateCredit: 'corporate_credit',
};

const _deliveryStatuses = {
  'pending': DeliveryStatus.pending,
  'assigned': DeliveryStatus.assigned,
  'dispatched': DeliveryStatus.dispatched,
  'in_transit': DeliveryStatus.inTransit,
  'delivered': DeliveryStatus.delivered,
  'failed': DeliveryStatus.failed,
  'cancelled': DeliveryStatus.cancelled,
};

PaymentMethod? paymentMethodFromApi(String? value) =>
    paymentMethodValues.entries.where((e) => e.value == value).map((e) => e.key).firstOrNull;

class ApiOrderRepository implements OrderRepository {
  ApiOrderRepository(this._api);
  final ApiClient _api;

  static const _path = '/api/me/orders';

  @override
  Future<List<FuelProduct>> getFuelProducts() async {
    final json = await _api.get('$_path/fuel-products') as List<dynamic>;
    return [
      for (final p in json)
        if (_fuelFromApi(p['fuelType'] as String?) case final type?)
          FuelProduct(type: type, pricePerLitre: (p['pricePerLitre'] as num).toDouble()),
    ];
  }

  @override
  Future<List<Order>> getOrders() async {
    final json = await _api.get(_path) as List<dynamic>;
    return json.map((o) => orderFromJson(o as Map<String, dynamic>)).toList();
  }

  @override
  Future<OrderQuote> quote(OrderDraft draft) async {
    final json = await _api.post('$_path/quote', {'lines': _linesToJson(draft.lines)}) as Map<String, dynamic>;
    return OrderQuote(
      subtotal: (json['subtotal'] as num).toDouble(),
      deliveryFee: (json['deliveryFee'] as num).toDouble(),
    );
  }

  @override
  Future<Order> placeOrder(OrderDraft draft) async =>
      orderFromJson(await _api.post(_path, draftToJson(draft)) as Map<String, dynamic>);

  @override
  Future<Order> cancelOrder(String id) async =>
      orderFromJson(await _api.post('$_path/$id/cancel', null) as Map<String, dynamic>);

  @override
  Future<Order> progressDelivery(String id) async =>
      orderFromJson(await _api.post('$_path/$id/progress-delivery', null) as Map<String, dynamic>);

  // Public for tests. corporateId isn't sent: company billing isn't in the API yet.
  static Map<String, dynamic> draftToJson(OrderDraft d) => {
        'deliveryAddress': d.address,
        'requestedDeliveryAt': d.scheduledAt.toUtc().toIso8601String(),
        'customerNotes': d.notes,
        'lines': _linesToJson(d.lines),
      };

  static List<Map<String, dynamic>> _linesToJson(List<OrderLineDraft> lines) => [
        for (final l in lines) {'customerVehicleId': l.vehicleId, 'fuelType': l.fuelType.name, 'quantity': l.litres},
      ];

  static Order orderFromJson(Map<String, dynamic> json) {
    final paymentMethod = paymentMethodFromApi(json['paymentMethod'] as String?);
    return Order(
      id: json['id'] as String,
      orderNumber: json['orderNumber'] as String,
      lines: [
        for (final l in json['lines'] as List<dynamic>)
          OrderLine(
            vehicleId: l['customerVehicleId'] as String? ?? '',
            vehicleName: l['vehicleName'] as String,
            registrationNumber: l['registrationNumber'] as String,
            fuelType: _fuelFromApi(l['fuelType'] as String?) ?? FuelType.diesel,
            litres: (l['quantity'] as num).toDouble(),
            unitPrice: (l['unitPrice'] as num).toDouble(),
            plateVerified: l['plateVerifiedAt'] != null,
          ),
      ],
      address: json['deliveryAddress'] as String,
      scheduledAt: _date(json['requestedDeliveryAt']),
      createdAt: _date(json['createdAt']),
      deliveryFee: (json['deliveryFee'] as num).toDouble(),
      status: _orderStatus(json['status'] as String),
      deliveryStatus: _deliveryStatuses[json['deliveryStatus']] ?? DeliveryStatus.pending,
      paymentStatus: PaymentStatus.values.asNameMap()[json['paymentStatus']] ?? PaymentStatus.unpaid,
      paymentMethod: paymentMethod,
      notes: json['customerNotes'] as String?,
      driverName: json['driverName'] as String?,
      truckRegistration: json['truckRegistration'] as String?,
      timeline: [
        for (final e in json['timeline'] as List<dynamic>) OrderEvent(e['title'] as String, _date(e['at'])),
      ],
      refund: json['refund'] != null
          ? OrderRefund(
              id: json['refund']['id'] as String,
              amount: (json['refund']['amount'] as num).toDouble(),
              status: json['refund']['status'] as String,
              providerReference: json['refund']['providerReference'] as String?,
              createdAt: _date(json['refund']['createdAt']),
            )
          : null,
    );
  }

  static DateTime _date(dynamic value) => DateTime.parse(value as String).toLocal();

  static FuelType? _fuelFromApi(String? value) => FuelType.values.asNameMap()[value];

  // partially_fulfilled has no app status yet; it is still being worked on.
  static OrderStatus _orderStatus(String value) =>
      OrderStatus.values.asNameMap()[value] ?? (value == 'partially_fulfilled' ? OrderStatus.processing : OrderStatus.pending);
}
