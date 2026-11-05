import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import 'package:instafuel_customer/data/api/api_client.dart';
import 'package:instafuel_customer/data/api/api_order_repository.dart';
import 'package:instafuel_customer/data/api/api_payment_repository.dart';
import 'package:instafuel_customer/data/repositories.dart';
import 'package:instafuel_customer/models/fuel.dart';
import 'package:instafuel_customer/models/order.dart';
import 'package:instafuel_customer/models/payment.dart';

// Shaped like a MyOrderResponse from GET /api/me/orders.
final _orderJson = {
  'id': 'o-1',
  'orderNumber': 'IF-261105-ABC123',
  'status': 'processing',
  'deliveryStatus': 'in_transit',
  'paymentStatus': 'paid',
  'paymentMethod': 'mobile_banking',
  'requestedDeliveryAt': '2026-11-07T04:00:00Z',
  'createdAt': '2026-11-05T03:50:21.066713Z',
  'deliveryAddress': 'House 1, Dhanmondi',
  'customerNotes': 'gate 2',
  'subtotal': 7260.00,
  'deliveryFee': 150,
  'totalAmount': 7410.00,
  'driverName': 'Rahim Uddin',
  'truckRegistration': 'DHAKA METRO-TA 11-2233',
  'lines': [
    {
      'customerVehicleId': 'v-1',
      'vehicleName': 'Car',
      'registrationNumber': 'DHAKA METRO-GA 12-3456',
      'fuelType': 'octane',
      'quantity': 20.000,
      'unitPrice': 135.00,
      'lineTotal': 2700.00,
      'plateVerifiedAt': '2026-11-07T04:10:00Z',
    },
    {
      'customerVehicleId': 'v-2',
      'vehicleName': 'Generator',
      'registrationNumber': 'GEN-1',
      'fuelType': 'diesel',
      'quantity': 40,
      'unitPrice': 114,
      'lineTotal': 4560,
      'plateVerifiedAt': null,
    },
  ],
  'timeline': [
    {'title': 'Order placed', 'at': '2026-11-05T03:50:21Z'},
    {'title': 'On the way', 'at': '2026-11-07T03:30:00Z'},
  ],
};

ApiClient _client(Future<http.Response> Function(http.Request) handler) =>
    ApiClient(baseUrl: 'http://api.test', httpClient: MockClient(handler))..token = 'jwt';

void main() {
  test('getOrders parses the API shape and maps snake_case values to enums', () async {
    final api = _client((_) async => http.Response(jsonEncode([_orderJson]), 200));

    final order = (await ApiOrderRepository(api).getOrders()).single;

    expect(order.status, OrderStatus.processing);
    expect(order.deliveryStatus, DeliveryStatus.inTransit);
    expect(order.paymentStatus, PaymentStatus.paid);
    expect(order.paymentMethod, PaymentMethod.mobileBanking);
    expect(order.scheduledAt.toUtc(), DateTime.utc(2026, 11, 7, 4));
    expect(order.total, 7410);
    expect(order.lines.map((l) => l.fuelType), [FuelType.octane, FuelType.diesel]);
    expect(order.lines.map((l) => l.plateVerified), [true, false]);
    expect(order.timeline.map((e) => e.title), ['Order placed', 'On the way']);
    expect(order.driverName, 'Rahim Uddin');
  });

  test('unknown statuses fall back instead of crashing', () {
    final order = ApiOrderRepository.orderFromJson({
      ..._orderJson,
      'status': 'partially_fulfilled',
      'deliveryStatus': 'some_future_status',
      'paymentMethod': null,
    });

    expect(order.status, OrderStatus.processing);
    expect(order.deliveryStatus, DeliveryStatus.pending);
    expect(order.paymentMethod, isNull);
  });

  test('placeOrder POSTs vehicle lines and a UTC delivery time', () async {
    late http.Request sent;
    final api = _client((request) async {
      sent = request;
      return http.Response(jsonEncode(_orderJson), 201);
    });

    await ApiOrderRepository(api).placeOrder(OrderDraft(
      lines: const [OrderLineDraft(vehicleId: 'v-1', fuelType: FuelType.octane, litres: 20)],
      address: 'House 1',
      scheduledAt: DateTime.utc(2026, 11, 7, 4),
      notes: 'gate 2',
      corporateId: 'c-1',
    ));

    expect(sent.method, 'POST');
    expect(sent.url.path, '/api/me/orders');
    expect(jsonDecode(sent.body), {
      'deliveryAddress': 'House 1',
      'requestedDeliveryAt': '2026-11-07T04:00:00.000Z',
      'customerNotes': 'gate 2',
      'lines': [
        {'customerVehicleId': 'v-1', 'fuelType': 'octane', 'quantity': 20.0},
      ],
    });
  });

  test('lead-time error (400) shows the API message', () async {
    final api = _client((_) async => http.Response('{"message":"Delivery must be at least 24 hours from now."}', 400));

    expect(
      () => ApiOrderRepository(api).placeOrder(OrderDraft(lines: const [], address: 'x', scheduledAt: DateTime.now())),
      throwsA(isA<AppException>().having((e) => e.message, 'message', 'Delivery must be at least 24 hours from now.')),
    );
  });

  test('pay sends the API method name and maps the result', () async {
    late http.Request sent;
    final api = _client((request) async {
      sent = request;
      return http.Response(
          jsonEncode({'orderId': 'o-1', 'status': 'pending', 'method': 'cash', 'amount': 7410, 'reference': null}), 200);
    });

    final result = await ApiPaymentRepository(api).pay(orderId: 'o-1', method: PaymentMethod.cashOnDelivery);

    expect(sent.url.path, '/api/me/orders/o-1/payments');
    final body = jsonDecode(sent.body) as Map<String, dynamic>;
    expect(body['method'], 'cash');
    expect(body['idempotencyKey'], startsWith('o-1-'));
    expect(result.status, PaymentStatus.pending);
    expect(result.method, PaymentMethod.cashOnDelivery);
    expect(result.isSuccess, isTrue);
  });
}
