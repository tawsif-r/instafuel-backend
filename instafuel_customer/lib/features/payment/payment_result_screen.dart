import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../core/widgets/responsive_center.dart';
import '../../models/payment.dart';

class PaymentResultScreen extends StatelessWidget {
  const PaymentResultScreen({super.key, required this.result});

  final PaymentResult result;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final ok = result.isSuccess;
    final payLater = result.status == PaymentStatus.pending;

    final title = !ok
        ? 'Payment failed'
        : payLater
            ? 'Order confirmed'
            : 'Payment successful';
    final message = !ok
        ? (result.message ?? 'Your payment could not be completed. No money was taken.')
        : payLater
            ? 'Pay ${formatMoney(result.amount)} by ${result.method.label.toLowerCase()}. We’ll notify you when the driver is assigned.'
            : '${formatMoney(result.amount)} paid by ${result.method.label}. We’ll notify you when the driver is assigned.';

    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppTheme.gap24),
          child: ResponsiveCenter(
            maxWidth: 440,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Spacer(),
                // Small pop-in animation for the result icon.
                TweenAnimationBuilder<double>(
                  tween: Tween(begin: 0.5, end: 1),
                  duration: const Duration(milliseconds: 400),
                  curve: Curves.easeOutBack,
                  builder: (_, scale, child) => Transform.scale(scale: scale, child: child),
                  child: CircleAvatar(
                    radius: 48,
                    backgroundColor: ok ? theme.colorScheme.primaryContainer : theme.colorScheme.errorContainer,
                    child: Icon(
                      ok ? Icons.check_rounded : Icons.close_rounded,
                      size: 56,
                      color: ok ? theme.colorScheme.onPrimaryContainer : theme.colorScheme.onErrorContainer,
                    ),
                  ),
                ),
                const SizedBox(height: AppTheme.gap24),
                Text(title, textAlign: TextAlign.center, style: theme.textTheme.headlineSmall),
                const SizedBox(height: AppTheme.gap8),
                Text(
                  message,
                  textAlign: TextAlign.center,
                  style: theme.textTheme.bodyLarge?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                ),
                if (result.reference != null) ...[
                  const SizedBox(height: AppTheme.gap16),
                  Text('Reference: ${result.reference}', textAlign: TextAlign.center, style: theme.textTheme.bodySmall),
                ],
                const Spacer(),
                if (ok)
                  FilledButton(
                    onPressed: () {
                      // Reset to Home, then open the order on top so "back" lands on Home.
                      context.go('/home');
                      context.push('/order/${result.orderId}');
                    },
                    child: const Text('Track order'),
                  )
                else ...[
                  FilledButton(
                    onPressed: () => context.pushReplacement('/order/${result.orderId}/pay'),
                    child: const Text('Try again'),
                  ),
                  const SizedBox(height: AppTheme.gap8),
                  OutlinedButton(
                    onPressed: () => context.pushReplacement('/order/${result.orderId}'),
                    child: const Text('Back to order'),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
