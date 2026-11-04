// Mock payments. Real card / mobile banking payments will redirect to a
// payment gateway; the app never handles card numbers itself.

import '../../models/order.dart';
import '../../models/payment.dart';
import '../repositories.dart';
import 'mock_database.dart';

class MockPaymentRepository implements PaymentRepository {
  MockPaymentRepository(this._db);
  final MockDatabase _db;

  @override
  Future<PaymentResult> pay({required String orderId, required PaymentMethod method}) async {
    await fakeDelay(1500);
    final index = _db.myOrders.indexWhere((o) => o.id == orderId);
    if (index == -1) throw const AppException('Order not found.');
    final order = _db.myOrders[index];

    PaymentResult result(PaymentStatus status, {String? message}) => PaymentResult(
          orderId: orderId,
          status: status,
          method: method,
          amount: order.total,
          reference: status == PaymentStatus.failed ? null : 'TXN${DateTime.now().millisecondsSinceEpoch % 1000000}',
          message: message,
        );

    late PaymentResult outcome;
    switch (method) {
      case PaymentMethod.cashOnDelivery:
      case PaymentMethod.bankTransfer:
        outcome = result(PaymentStatus.pending); // confirmed later (driver / finance)
      case PaymentMethod.card:
      case PaymentMethod.mobileBanking:
        outcome = result(PaymentStatus.paid);
      case PaymentMethod.corporateCredit:
        final i = _db.myAffiliations.indexWhere((a) => a.id == order.corporateId && a.isActive);
        if (i == -1) {
          outcome = result(PaymentStatus.failed, message: 'No active company account linked to this order.');
        } else if (_db.myAffiliations[i].availableCredit < order.total) {
          outcome = result(PaymentStatus.failed, message: 'Not enough company credit left for this order.');
        } else {
          final a = _db.myAffiliations[i];
          _db.myAffiliations[i] = a.copyWith(usedCredit: a.usedCredit + order.total);
          outcome = result(PaymentStatus.paid);
        }
    }

    final event = switch (outcome.status) {
      PaymentStatus.paid => 'Payment received (${method.label})',
      PaymentStatus.pending => 'Payment on delivery (${method.label})',
      _ => 'Payment failed',
    };
    _db.myOrders[index] = order.copyWith(
      status: outcome.isSuccess ? OrderStatus.confirmed : order.status,
      paymentStatus: outcome.status,
      paymentMethod: method,
      timeline: [...order.timeline, OrderEvent(event, DateTime.now())],
    );
    return outcome;
  }
}
