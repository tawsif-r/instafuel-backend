import 'package:flutter/material.dart';

import '../../models/order.dart';

/// Horizontal progress through the delivery steps (pending -> delivered).
class DeliveryProgress extends StatelessWidget {
  const DeliveryProgress({super.key, required this.status});

  final DeliveryStatus status;

  static const _icons = {
    DeliveryStatus.pending: Icons.receipt_long_outlined,
    DeliveryStatus.assigned: Icons.person_pin_outlined,
    DeliveryStatus.dispatched: Icons.local_shipping_outlined,
    DeliveryStatus.inTransit: Icons.route_outlined,
    DeliveryStatus.delivered: Icons.local_gas_station_outlined,
  };

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final current = DeliveryStatus.steps.indexOf(status); // -1 for failed/cancelled
    final steps = DeliveryStatus.steps;

    return Row(
      children: [
        for (var i = 0; i < steps.length; i++) ...[
          Tooltip(
            message: steps[i].label,
            child: CircleAvatar(
              radius: 16,
              backgroundColor: i <= current ? scheme.primary : scheme.surfaceContainerHighest,
              child: Icon(
                _icons[steps[i]],
                size: 18,
                color: i <= current ? scheme.onPrimary : scheme.onSurfaceVariant,
              ),
            ),
          ),
          if (i < steps.length - 1)
            Expanded(
              child: Container(
                height: 3,
                margin: const EdgeInsets.symmetric(horizontal: 4),
                decoration: BoxDecoration(
                  color: i < current ? scheme.primary : scheme.surfaceContainerHighest,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
        ],
      ],
    );
  }
}
