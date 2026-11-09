// Corporate & Fleet Billing Screen conforming to SOP-OPS-001 Section 4.2 & Section 12.3.
// Features: Digital Fleet Credit Card, Real-time Credit Utilization Bar, 30-Day Invoicing Status.

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../core/widgets/async_value_view.dart';
import '../../core/widgets/empty_state.dart';
import '../../core/widgets/responsive_center.dart';
import '../../models/corporate.dart';
import 'corporate_controller.dart';
import 'join_company_sheet.dart';

class CorporateScreen extends ConsumerWidget {
  const CorporateScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final affiliations = ref.watch(affiliationsProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Corporate & Fleet Accounts'),
        actions: [
          IconButton(
            tooltip: 'SOP Credit Policy',
            icon: const Icon(Icons.info_outline_rounded),
            onPressed: () => showDialog(
              context: context,
              builder: (ctx) => AlertDialog(
                title: const Text('Corporate Credit Terms (SOP §12.3)'),
                content: const Text(
                  'Corporate and institutional customers receive approved 30-day credit limits for fleet and backup generator fueling. Invoices are consolidated monthly with detailed delivery manifests.',
                ),
                actions: [
                  TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Close')),
                ],
              ),
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => JoinCompanySheet.show(context),
        backgroundColor: const Color(0xFF059669),
        foregroundColor: Colors.white,
        icon: const Icon(Icons.add_business_rounded),
        label: const Text('Link Company Account', style: TextStyle(fontWeight: FontWeight.bold)),
      ),
      body: AsyncValueView(
        value: affiliations,
        onRetry: () => ref.invalidate(affiliationsProvider),
        data: (list) => list.isEmpty
            ? EmptyState(
                icon: Icons.business_outlined,
                title: 'No Corporate Account Linked',
                message: 'Link your company or institution to bill fuel orders to its approved credit account with 30-day terms (SOP §12.3).',
                action: FilledButton.icon(
                  onPressed: () => JoinCompanySheet.show(context),
                  style: FilledButton.styleFrom(backgroundColor: const Color(0xFF059669)),
                  icon: const Icon(Icons.add_business_rounded),
                  label: const Text('Enter Company Code'),
                ),
              )
            : RefreshIndicator(
                onRefresh: () => ref.refresh(affiliationsProvider.future),
                child: ListView(
                  padding: const EdgeInsets.fromLTRB(AppTheme.gap16, AppTheme.gap16, AppTheme.gap16, 96),
                  children: [
                    ResponsiveCenter(
                      maxWidth: 680,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          for (final a in list) ...[
                            _CorporateFleetCard(affiliation: a),
                            const SizedBox(height: AppTheme.gap16),
                            _CorporateDetailCard(affiliation: a),
                            const SizedBox(height: AppTheme.gap24),
                          ],
                        ],
                      ),
                    ),
                  ],
                ),
              ),
      ),
    );
  }
}

/// Embossed Corporate Fleet Card (FuelBuddy / Amex Corporate inspired)
class _CorporateFleetCard extends StatelessWidget {
  const _CorporateFleetCard({required this.affiliation});

  final CorporateAffiliation affiliation;

  @override
  Widget build(BuildContext context) {
    final a = affiliation;
    final usedShare = a.creditLimit == 0 ? 0.0 : (a.usedCredit / a.creditLimit).clamp(0.0, 1.0);

    return Container(
      padding: const EdgeInsets.all(AppTheme.gap24),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [
            Color(0xFF064E3B),
            Color(0xFF0B1320),
            Color(0xFF0F172A),
          ],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF064E3B).withValues(alpha: 0.35),
            blurRadius: 20,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Card Header: Brand & Status
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Container(
                    width: 38,
                    height: 28,
                    decoration: BoxDecoration(
                      color: const Color(0xFFF59E0B),
                      borderRadius: BorderRadius.circular(6),
                      gradient: const LinearGradient(
                        colors: [Color(0xFFFDE68A), Color(0xFFD97706)],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      ),
                    ),
                    child: const Icon(Icons.memory_rounded, size: 20, color: Color(0xFF78350F)),
                  ),
                  const SizedBox(width: 8),
                  const Icon(Icons.contactless_rounded, color: Colors.white70, size: 20),
                ],
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: a.isActive
                      ? const Color(0xFF10B981).withValues(alpha: 0.2)
                      : const Color(0xFFF59E0B).withValues(alpha: 0.2),
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(
                    color: a.isActive ? const Color(0xFF10B981) : const Color(0xFFF59E0B),
                    width: 1,
                  ),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      a.isActive ? Icons.check_circle_rounded : Icons.pending_rounded,
                      size: 14,
                      color: a.isActive ? const Color(0xFF34D399) : const Color(0xFFFBBF24),
                    ),
                    const SizedBox(width: 5),
                    Text(
                      a.isActive ? 'ACTIVE FLEET LINE' : 'PENDING AUDIT',
                      style: TextStyle(
                        color: a.isActive ? const Color(0xFF34D399) : const Color(0xFFFBBF24),
                        fontWeight: FontWeight.w800,
                        fontSize: 11,
                        letterSpacing: 0.5,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),

          const SizedBox(height: AppTheme.gap24),

          // Available Balance Hero
          const Text(
            'AVAILABLE FLEET CREDIT',
            style: TextStyle(
              color: Colors.white60,
              fontSize: 11,
              fontWeight: FontWeight.bold,
              letterSpacing: 1.2,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            formatMoney(a.availableCredit),
            style: const TextStyle(
              color: Colors.white,
              fontSize: 32,
              fontWeight: FontWeight.w900,
              letterSpacing: -0.5,
            ),
          ),

          const SizedBox(height: AppTheme.gap16),

          // Credit Meter
          ClipRRect(
            borderRadius: BorderRadius.circular(6),
            child: LinearProgressIndicator(
              value: usedShare,
              minHeight: 6,
              backgroundColor: Colors.white24,
              valueColor: AlwaysStoppedAnimation(
                usedShare > 0.85 ? const Color(0xFFEF4444) : const Color(0xFF34D399),
              ),
            ),
          ),
          const SizedBox(height: 6),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Used: ${formatMoney(a.usedCredit)}',
                style: const TextStyle(color: Colors.white70, fontSize: 11),
              ),
              Text(
                'Limit: ${formatMoney(a.creditLimit)}',
                style: const TextStyle(color: Colors.white70, fontSize: 11, fontWeight: FontWeight.bold),
              ),
            ],
          ),

          const SizedBox(height: AppTheme.gap20),

          // Card Footer
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('COMPANY', style: TextStyle(color: Colors.white54, fontSize: 10, letterSpacing: 0.8)),
                  Text(
                    a.companyName,
                    style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w800, fontSize: 14),
                  ),
                ],
              ),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  const Text('MEMBER ROLE', style: TextStyle(color: Colors.white54, fontSize: 10, letterSpacing: 0.8)),
                  Text(
                    a.role.label,
                    style: const TextStyle(color: Color(0xFF34D399), fontWeight: FontWeight.w800, fontSize: 13),
                  ),
                ],
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _CorporateDetailCard extends StatelessWidget {
  const _CorporateDetailCard({required this.affiliation});

  final CorporateAffiliation affiliation;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final a = affiliation;

    return Container(
      padding: const EdgeInsets.all(AppTheme.gap20),
      decoration: BoxDecoration(
        color: isDark ? const Color(0xFF1E293B) : Colors.white,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(
          color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Billing & Operational Details (SOP §12.3)',
            style: TextStyle(fontWeight: FontWeight.w800, fontSize: 15),
          ),
          const SizedBox(height: AppTheme.gap14),
          _PolicyRow(
            icon: Icons.calendar_today_rounded,
            title: '30-Day Consolidated Cycle',
            subtitle: 'Monthly tax invoice issued with calibrated delivery manifests.',
          ),
          const SizedBox(height: AppTheme.gap12),
          _PolicyRow(
            icon: Icons.local_gas_station_rounded,
            title: 'Fleet & Generator Direct Fueling',
            subtitle: 'Pre-approved corporate vehicles bypass manual checkout payments.',
          ),
          const SizedBox(height: AppTheme.gap12),
          _PolicyRow(
            icon: Icons.verified_user_outlined,
            title: 'Authorized Signatory Control',
            subtitle: 'Only verified representatives (${a.role.label}) can confirm dispatches.',
          ),
          const SizedBox(height: AppTheme.gap20),
          Row(
            children: [
              Expanded(
                child: FilledButton.icon(
                  onPressed: () => context.push('/new-order'),
                  style: FilledButton.styleFrom(
                    backgroundColor: const Color(0xFF059669),
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                  ),
                  icon: const Icon(Icons.bolt_rounded, size: 18),
                  label: const Text('Schedule Fleet Refuel'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _PolicyRow extends StatelessWidget {
  const _PolicyRow({required this.icon, required this.title, required this.subtitle});

  final IconData icon;
  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          width: 36,
          height: 36,
          decoration: BoxDecoration(
            color: const Color(0xFF059669).withValues(alpha: 0.12),
            borderRadius: BorderRadius.circular(10),
          ),
          child: Icon(icon, color: const Color(0xFF059669), size: 18),
        ),
        const SizedBox(width: AppTheme.gap12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
              const SizedBox(height: 2),
              Text(
                subtitle,
                style: TextStyle(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                  fontSize: 12,
                  height: 1.3,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
