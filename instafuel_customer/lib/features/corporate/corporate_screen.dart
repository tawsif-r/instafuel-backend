import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../core/widgets/async_value_view.dart';
import '../../core/widgets/empty_state.dart';
import '../../core/widgets/responsive_center.dart';
import '../../core/widgets/status_chip.dart';
import '../../models/corporate.dart';
import 'corporate_controller.dart';
import 'join_company_sheet.dart';

class CorporateScreen extends ConsumerWidget {
  const CorporateScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final affiliations = ref.watch(affiliationsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Corporate accounts')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => JoinCompanySheet.show(context),
        icon: const Icon(Icons.add_business_outlined),
        label: const Text('Join a company'),
      ),
      body: AsyncValueView(
        value: affiliations,
        onRetry: () => ref.invalidate(affiliationsProvider),
        data: (list) => list.isEmpty
            ? const EmptyState(
                icon: Icons.business_outlined,
                title: 'No company linked',
                message: 'Link your company to bill fuel orders to its approved credit account.',
              )
            : ListView.separated(
                padding: const EdgeInsets.fromLTRB(AppTheme.gap16, AppTheme.gap16, AppTheme.gap16, 96),
                itemCount: list.length,
                separatorBuilder: (_, _) => const SizedBox(height: AppTheme.gap12),
                itemBuilder: (_, i) => ResponsiveCenter(child: _AffiliationCard(affiliation: list[i])),
              ),
      ),
    );
  }
}

class _AffiliationCard extends StatelessWidget {
  const _AffiliationCard({required this.affiliation});

  final CorporateAffiliation affiliation;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final a = affiliation;
    final usedShare = a.creditLimit == 0 ? 0.0 : (a.usedCredit / a.creditLimit).clamp(0.0, 1.0);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.gap16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                CircleAvatar(
                  backgroundColor: theme.colorScheme.tertiaryContainer,
                  child: Icon(Icons.business, color: theme.colorScheme.onTertiaryContainer),
                ),
                const SizedBox(width: AppTheme.gap12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(a.companyName, style: theme.textTheme.titleMedium),
                      Text(a.role.label, style: theme.textTheme.bodySmall),
                    ],
                  ),
                ),
                StatusChip.affiliation(a.status),
              ],
            ),
            if (a.isActive) ...[
              const SizedBox(height: AppTheme.gap16),
              Row(
                children: [
                  Expanded(child: Text('Credit used', style: theme.textTheme.bodySmall)),
                  Text('${formatMoney(a.usedCredit)} of ${formatMoney(a.creditLimit)}', style: theme.textTheme.bodySmall),
                ],
              ),
              const SizedBox(height: AppTheme.gap8),
              ClipRRect(
                borderRadius: BorderRadius.circular(4),
                child: LinearProgressIndicator(value: usedShare, minHeight: 8),
              ),
              const SizedBox(height: AppTheme.gap8),
              Text('${formatMoney(a.availableCredit)} available', style: theme.textTheme.titleSmall),
            ] else ...[
              const SizedBox(height: AppTheme.gap12),
              Text(
                'Your company needs to approve this link before you can bill orders to it.',
                style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
