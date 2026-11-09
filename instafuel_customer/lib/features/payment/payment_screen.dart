// Payment settlement adhering to SOP-OPS-001 Section 12 (Payment, Credit & Collection).
// Supports advance digital payment (Card, MFS), Bank Transfer, and approved Corporate Credit.

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../core/widgets/async_value_view.dart';
import '../../core/widgets/empty_state.dart';
import '../../core/widgets/responsive_center.dart';
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
      appBar: AppBar(title: const Text('Invoice Settlement (SOP §12)')),
      body: AsyncValueView(
        value: orderValue,
        data: (order) {
          if (order == null) return const EmptyState(icon: Icons.search_off, title: 'Order not found');

          final company = affiliations.where((a) => a.id == order.corporateId && a.isActive).firstOrNull;
          final methods = PaymentMethod.values
              .where((m) => m != PaymentMethod.corporateCredit || company != null)
              .toList();
          _method ??= company != null ? PaymentMethod.corporateCredit : PaymentMethod.card;

          return ListView(
            padding: const EdgeInsets.all(AppTheme.gap16),
            children: [
              ResponsiveCenter(
                maxWidth: 580,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _InvoiceSummaryHeader(order: order),
                    const SizedBox(height: AppTheme.gap20),
                    const Text(
                      'Select Approved Payment Method (SOP §12.1)',
                      style: TextStyle(fontWeight: FontWeight.w800, fontSize: 14),
                    ),
                    const SizedBox(height: AppTheme.gap12),
                    for (final m in methods) ...[
                      _PaymentMethodTile(
                        method: m,
                        selected: _method == m,
                        extra: m == PaymentMethod.corporateCredit && company != null ? _creditInfo(company, order) : null,
                        onTap: _paying ? null : () => setState(() => _method = m),
                      ),
                      const SizedBox(height: AppTheme.gap10),
                    ],
                    const SizedBox(height: AppTheme.gap8),
                    if (_method == PaymentMethod.card || _method == PaymentMethod.mobileBanking)
                      _SecurityBadge(
                        icon: Icons.lock_outline_rounded,
                        text: '256-bit encrypted gateway. Instafuel never stores your PIN or CVV.',
                      ),
                    if (_method == PaymentMethod.bankTransfer)
                      _SecurityBadge(
                        icon: Icons.account_balance_outlined,
                        text: 'Direct BEFTN / RTGS transfer. Order confirmed upon finance reconciliation (SOP §12.2).',
                      ),
                    if (_method == PaymentMethod.corporateCredit)
                      _SecurityBadge(
                        icon: Icons.verified_user_outlined,
                        text: 'Approved corporate credit line. Consolidated monthly billing (SOP §12.3).',
                      ),
                    const SizedBox(height: AppTheme.gap24),
                    FilledButton.icon(
                      onPressed: _method == null || _paying ? null : () => _pay(order),
                      style: FilledButton.styleFrom(
                        backgroundColor: const Color(0xFF059669),
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                      ),
                      icon: _paying
                          ? const SizedBox.square(dimension: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                          : const Icon(Icons.check_circle_outline_rounded, size: 20),
                      label: Text(
                        _method == PaymentMethod.corporateCredit
                            ? 'Charge to Corporate Account'
                            : 'Pay Now (${formatMoney(order.total)})',
                        style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                      ),
                    ),
                    const SizedBox(height: AppTheme.gap8),
                    TextButton(
                      onPressed: _paying ? null : () => context.pushReplacement('/order/${order.id}'),
                      child: const Text('Review Order Manifest', style: TextStyle(fontWeight: FontWeight.bold)),
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
      '${c.companyName} · ${formatMoney(c.availableCredit)} credit available',
      style: TextStyle(
        color: enough ? const Color(0xFF059669) : const Color(0xFFDC2626),
        fontWeight: FontWeight.bold,
        fontSize: 12,
      ),
    );
  }
}

class _InvoiceSummaryHeader extends StatelessWidget {
  const _InvoiceSummaryHeader({required this.order});

  final Order order;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AppTheme.gap20),
      decoration: BoxDecoration(
        color: const Color(0xFF0F172A),
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.1),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'COMMERCIAL INVOICE',
                style: TextStyle(
                  color: Colors.white.withValues(alpha: 0.7),
                  fontSize: 11,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 0.6,
                ),
              ),
              Text(
                order.orderNumber,
                style: const TextStyle(color: Color(0xFF34D399), fontWeight: FontWeight.bold, fontSize: 13),
              ),
            ],
          ),
          const SizedBox(height: AppTheme.gap12),
          Text(
            formatMoney(order.total),
            style: const TextStyle(
              color: Colors.white,
              fontSize: 28,
              fontWeight: FontWeight.w900,
              letterSpacing: -0.6,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            '${formatLitres(order.totalLitres)} Total Fuel · ${order.lines.length} vehicle(s) · ${order.address}',
            style: TextStyle(color: Colors.white.withValues(alpha: 0.65), fontSize: 12.5),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );
  }
}

class _PaymentMethodTile extends StatelessWidget {
  const _PaymentMethodTile({
    required this.method,
    required this.selected,
    required this.onTap,
    this.extra,
  });

  final PaymentMethod method;
  final bool selected;
  final VoidCallback? onTap;
  final Widget? extra;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: selected
              ? const Color(0xFF059669).withValues(alpha: isDark ? 0.2 : 0.08)
              : (isDark ? const Color(0xFF1E293B) : Colors.white),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: selected ? const Color(0xFF059669) : (isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
            width: selected ? 2 : 1,
          ),
        ),
        child: Row(
          children: [
            Container(
              width: 42,
              height: 42,
              decoration: BoxDecoration(
                color: selected ? const Color(0xFF059669) : Colors.grey.withValues(alpha: 0.15),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(method.icon, color: selected ? Colors.white : Colors.grey, size: 22),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(method.label, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
                  const SizedBox(height: 2),
                  extra ?? Text(method.description, style: TextStyle(color: theme.colorScheme.onSurfaceVariant, fontSize: 11.5)),
                ],
              ),
            ),
            Icon(
              selected ? Icons.radio_button_checked_rounded : Icons.radio_button_off_rounded,
              color: selected ? const Color(0xFF059669) : Colors.grey,
            ),
          ],
        ),
      ),
    );
  }
}

class _SecurityBadge extends StatelessWidget {
  const _SecurityBadge({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: const Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 18, color: const Color(0xFF059669)),
          const SizedBox(width: 8),
          Expanded(
            child: Text(text, style: const TextStyle(fontSize: 11.5, color: Color(0xFF475569))),
          ),
        ],
      ),
    );
  }
}
