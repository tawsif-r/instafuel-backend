import 'package:flutter/material.dart';

import '../../models/corporate.dart';
import '../../models/order.dart';
import '../../models/payment.dart';
import '../../models/vehicle.dart';

enum StatusTone { neutral, info, success, warning, danger }

/// Small coloured pill for statuses. Use the named constructors so the
/// status -> colour mapping lives in one place.
class StatusChip extends StatelessWidget {
  const StatusChip({super.key, required this.label, this.tone = StatusTone.neutral, this.icon});

  final String label;
  final StatusTone tone;
  final IconData? icon;

  factory StatusChip.delivery(DeliveryStatus s) => StatusChip(
        label: s.label,
        tone: switch (s) {
          DeliveryStatus.delivered => StatusTone.success,
          DeliveryStatus.failed || DeliveryStatus.cancelled => StatusTone.danger,
          DeliveryStatus.pending => StatusTone.neutral,
          _ => StatusTone.info,
        },
      );

  factory StatusChip.order(OrderStatus s) => StatusChip(
        label: s.label,
        tone: switch (s) {
          OrderStatus.completed => StatusTone.success,
          OrderStatus.cancelled || OrderStatus.failed => StatusTone.danger,
          OrderStatus.pending => StatusTone.warning,
          _ => StatusTone.info,
        },
      );

  factory StatusChip.payment(PaymentStatus s) => StatusChip(
        label: s.label,
        tone: switch (s) {
          PaymentStatus.paid => StatusTone.success,
          PaymentStatus.refunded => StatusTone.info,
          PaymentStatus.failed => StatusTone.danger,
          PaymentStatus.unpaid => StatusTone.warning,
          PaymentStatus.pending => StatusTone.neutral,
        },
      );

  factory StatusChip.verification(VerificationStatus s) => StatusChip(
        label: s.label,
        icon: switch (s) {
          VerificationStatus.verified => Icons.verified_outlined,
          VerificationStatus.rejected => Icons.error_outline,
          VerificationStatus.pending => Icons.hourglass_top,
        },
        tone: switch (s) {
          VerificationStatus.verified => StatusTone.success,
          VerificationStatus.rejected => StatusTone.danger,
          VerificationStatus.pending => StatusTone.warning,
        },
      );

  factory StatusChip.affiliation(AffiliationStatus s) => StatusChip(
        label: s.label,
        tone: s == AffiliationStatus.active ? StatusTone.success : StatusTone.warning,
      );

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final dark = Theme.of(context).brightness == Brightness.dark;

    // Fixed hues for success/warning so they read the same in both themes.
    final (Color bg, Color fg) = switch (tone) {
      StatusTone.neutral => (scheme.surfaceContainerHighest, scheme.onSurfaceVariant),
      StatusTone.info => (scheme.secondaryContainer, scheme.onSecondaryContainer),
      StatusTone.success => dark
          ? (const Color(0xFF1E4620), const Color(0xFFB7F0B1))
          : (const Color(0xFFDDF5DA), const Color(0xFF1B5E20)),
      StatusTone.warning => dark
          ? (const Color(0xFF4A3B00), const Color(0xFFFFE08A))
          : (const Color(0xFFFFF1C2), const Color(0xFF6D4C00)),
      StatusTone.danger => (scheme.errorContainer, scheme.onErrorContainer),
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(999)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[Icon(icon, size: 14, color: fg), const SizedBox(width: 4)],
          Text(label, style: Theme.of(context).textTheme.labelMedium?.copyWith(color: fg)),
        ],
      ),
    );
  }
}
