// Choose how to pay for an order. Card / mobile banking will open a payment
// gateway page later; for now the mock repository returns a result directly.

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../core/widgets/async_value_view.dart';
import '../../core/widgets/empty_state.dart';
import '../../core/widgets/responsive_center.dart';
import '../../core/widgets/section_card.dart';
import '../../data/providers.dart';
import '../../models/corporate.dart';
import '../../models/order.dart';
import '../../models/payment.dart';
import '../corporate/corporate_controller.dart';
import '../orders/orders_controller.dart';

class PaymentScreen extends ConsumerStatefulWidget {
  const PaymentScreen({super.key, required this.orderId});

  final String orderId;

  @override
  ConsumerState<PaymentScreen> createState() => _PaymentScreenState();
}

class _PaymentScreenState extends ConsumerState<PaymentScreen> {
  PaymentMethod? _method;
  bool _paying = false;

  Future<void> _pay(Order order) async {
    setState(() => _paying = true);
    try {
      final result = await ref.read(paymentRepositoryProvider).pay(orderId: order.id, method: _method!);
      ref.read(ordersProvider.notifier).refresh();
      ref.read(affiliationsProvider.notifier).refresh();
      if (mounted) context.pushReplacement('/order/${order.id}/payment-result', extra: result);
    } catch (e) {
      if (mounted) {
        setState(() => _paying = false);
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final orderValue = ref.watch(orderByIdProvider(widget.orderId));
    final affiliations = ref.watch(affiliationsProvider).value ?? [];

    return Scaffold(
      appBar: AppBar(title: const Text('Payment')),
      body: AsyncValueView(
        value: orderValue,
        data: (order) {
          if (order == null) return const EmptyState(icon: Icons.search_off, title: 'Order not found');

          // Corporate credit is only offered when the order is billed to an active company.
          final company = affiliations.where((a) => a.id == order.corporateId && a.isActive).firstOrNull;
          final methods = PaymentMethod.values
              .where((m) => m != PaymentMethod.corporateCredit || company != null)
              .toList();
          _method ??= company != null ? PaymentMethod.corporateCredit : null;

          return ListView(
            padding: const EdgeInsets.all(AppTheme.gap16),
            children: [
              ResponsiveCenter(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _Summary(order: order),
                    const SizedBox(height: AppTheme.gap24),
                    Text('Payment method', style: Theme.of(context).textTheme.titleMedium),
                    const SizedBox(height: AppTheme.gap8),
                    for (final m in methods) ...[
                      _MethodCard(
                        method: m,
                        selected: _method == m,
                        extra: m == PaymentMethod.corporateCredit && company != null ? _creditInfo(company, order) : null,
                        onTap: _paying ? null : () => setState(() => _method = m),
                      ),
                      const SizedBox(height: AppTheme.gap8),
                    ],
                    if (_method == PaymentMethod.card || _method == PaymentMethod.mobileBanking)
                      _Note(
                        icon: Icons.lock_outline,
                        text: 'You will be taken to a secure payment page. '
                            'Instafuel never stores your card or wallet PIN.',
                      ),
                    if (_method == PaymentMethod.bankTransfer)
                      const _Note(
                        icon: Icons.info_outline,
                        text: 'Bank details will be shown after you confirm. '
                            'The order is confirmed once finance receives the transfer.',
                      ),
                    const SizedBox(height: AppTheme.gap24),
                    FilledButton.icon(
                      onPressed: _method == null || _paying ? null : () => _pay(order),
                      icon: _paying
                          ? const SizedBox.square(dimension: 18, child: CircularProgressIndicator(strokeWidth: 2))
                          : const Icon(Icons.lock_outline),
                      label: Text(
                        _method == PaymentMethod.cashOnDelivery
                            ? 'Confirm order'
                            : 'Pay ${formatMoney(order.total)}',
                      ),
                    ),
                    const SizedBox(height: AppTheme.gap8),
                    TextButton(
                      onPressed: _paying ? null : () => context.pushReplacement('/order/${order.id}'),
                      child: const Text('Pay later'),
                    ),
                  ],
                ),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _creditInfo(CorporateAffiliation c, Order order) {
    final enough = c.availableCredit >= order.total;
    return Text(
      '${c.companyName} · ${formatMoney(c.availableCredit)} available',
      style: TextStyle(color: enough ? null : Theme.of(context).colorScheme.error),
    );
  }
}

class _Summary extends StatelessWidget {
  const _Summary({required this.order});

  final Order order;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return SectionCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Amount to pay', style: theme.textTheme.bodyMedium),
          const SizedBox(height: AppTheme.gap4),
          Text(formatMoney(order.total), style: theme.textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w700)),
          const SizedBox(height: AppTheme.gap8),
          Text(
            '${order.orderNumber} · ${formatLitres(order.totalLitres)} · '
            '${order.lines.length} vehicle${order.lines.length == 1 ? '' : 's'}',
            style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          ),
        ],
      ),
    );
  }
}

class _MethodCard extends StatelessWidget {
  const _MethodCard({required this.method, required this.selected, required this.onTap, this.extra});

  final PaymentMethod method;
  final bool selected;
  final VoidCallback? onTap;
  final Widget? extra;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Semantics(
      selected: selected,
      button: true,
      child: Card(
        clipBehavior: Clip.antiAlias,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppTheme.radius),
          side: BorderSide(color: selected ? scheme.primary : scheme.outlineVariant, width: selected ? 2 : 1),
        ),
        child: ListTile(
          onTap: onTap,
          leading: Icon(method.icon, color: selected ? scheme.primary : null),
          title: Text(method.label),
          subtitle: extra ?? Text(method.description),
          trailing: Icon(
            selected ? Icons.radio_button_checked : Icons.radio_button_off,
            color: selected ? scheme.primary : scheme.outline,
          ),
        ),
      ),
    );
  }
}

class _Note extends StatelessWidget {
  const _Note({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(top: AppTheme.gap8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 18, color: theme.colorScheme.onSurfaceVariant),
          const SizedBox(width: AppTheme.gap8),
          Expanded(
            child: Text(text, style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
          ),
        ],
      ),
    );
  }
}
