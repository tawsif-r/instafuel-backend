import 'package:flutter/material.dart';

enum PaymentMethod {
  cashOnDelivery('Cash on delivery', 'Pay the driver when fuel is delivered', Icons.payments_outlined),
  card('Card', 'Visa, Mastercard, Amex via secure gateway', Icons.credit_card),
  mobileBanking('Mobile banking', 'Pay from your mobile wallet', Icons.phone_iphone),
  bankTransfer('Bank transfer', 'Transfer to our account, confirmed by finance', Icons.account_balance_outlined),
  corporateCredit('Corporate credit', 'Bill to your company account', Icons.business_center_outlined);

  const PaymentMethod(this.label, this.description, this.icon);
  final String label;
  final String description;
  final IconData icon;
}

enum PaymentStatus {
  unpaid('Unpaid'),
  pending('Pay on delivery'), // cash / bank transfer waiting for confirmation
  paid('Paid'),
  refunded('Refunded'),
  failed('Payment failed');

  const PaymentStatus(this.label);
  final String label;
}

class PaymentResult {
  const PaymentResult({
    required this.orderId,
    required this.status,
    required this.method,
    required this.amount,
    this.reference,
    this.message,
  });

  final String orderId;
  final PaymentStatus status;
  final PaymentMethod method;
  final double amount;
  final String? reference; // gateway / transaction reference
  final String? message;   // reason when it failed

  bool get isSuccess => status != PaymentStatus.failed;
}
