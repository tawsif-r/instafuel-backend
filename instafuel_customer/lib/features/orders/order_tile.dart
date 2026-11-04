import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../core/widgets/status_chip.dart';
import '../../models/order.dart';

/// Compact order row used on the dashboard and the orders list.
class OrderTile extends StatelessWidget {
  const OrderTile({super.key, required this.order});

  final Order order;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final vehicles = order.lines.length == 1 ? order.lines.first.vehicleName : '${order.lines.length} vehicles';

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push('/order/${order.id}'),
        child: Padding(
          padding: const EdgeInsets.all(AppTheme.gap16),
          child: Row(
            children: [
              CircleAvatar(
                backgroundColor: theme.colorScheme.primaryContainer,
                child: Icon(Icons.local_gas_station_outlined, color: theme.colorScheme.onPrimaryContainer),
              ),
              const SizedBox(width: AppTheme.gap16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('${order.orderNumber} · $vehicles', style: theme.textTheme.titleSmall),
                    const SizedBox(height: 2),
                    Text(
                      '${formatLitres(order.totalLitres)} · ${formatDate(order.scheduledAt)}, ${formatTime(order.scheduledAt)}',
                      style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                    ),
                    const SizedBox(height: AppTheme.gap8),
                    order.isActive ? StatusChip.delivery(order.deliveryStatus) : StatusChip.order(order.status),
                  ],
                ),
              ),
              Text(formatMoney(order.total), style: theme.textTheme.titleSmall),
            ],
          ),
        ),
      ),
    );
  }
}
