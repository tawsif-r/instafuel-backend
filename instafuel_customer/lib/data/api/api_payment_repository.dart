// Payments against Instafuel.Api (POST /api/me/orders/{id}/payments).
// Card and mobile banking will later redirect to a payment gateway; until then
// the API confirms them straight away in Development.

import '../../models/payment.dart';
import '../repositories.dart';
import 'api_client.dart';
import 'api_order_repository.dart';

class ApiPaymentRepository implements PaymentRepository {
  ApiPaymentRepository(this._api);
  final ApiClient _api;

  @override
  Future<PaymentResult> pay({required String orderId, required PaymentMethod method}) async {
    // A new key per tap. The API returns the same payment if this exact request is retried.
    final key = '$orderId-${DateTime.now().microsecondsSinceEpoch}';
    final json = await _api.post('/api/me/orders/$orderId/payments', {
      'method': paymentMethodValues[method],
      'idempotencyKey': key,
    }) as Map<String, dynamic>;
    return paymentResultFromJson(json);
  }

  // Public for tests.
  static PaymentResult paymentResultFromJson(Map<String, dynamic> json) => PaymentResult(
        orderId: json['orderId'] as String,
        status: switch (json['status']) {
          'successful' => PaymentStatus.paid,
          'pending' => PaymentStatus.pending,
          _ => PaymentStatus.failed,
        },
        method: paymentMethodFromApi(json['method'] as String?) ?? PaymentMethod.cashOnDelivery,
        amount: (json['amount'] as num).toDouble(),
        reference: json['reference'] as String?,
        message: json['message'] as String?,
      );
}
