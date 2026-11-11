import 'package:flutter/material.dart';

import '../../models/order.dart';

/// FuelBuddy/CAFU style animated and illuminated delivery progress bar.
class DeliveryProgress extends StatelessWidget {
  const DeliveryProgress({super.key, required this.status});

  final DeliveryStatus status;

  static const _stepDetails = [
    (DeliveryStatus.pending, 'Booked', Icons.check_circle_outline_rounded),
    (DeliveryStatus.assigned, 'Assigned', Icons.assignment_ind_rounded),
    (DeliveryStatus.dispatched, 'Dispatched', Icons.local_shipping_rounded),
    (DeliveryStatus.inTransit, 'En Route', Icons.navigation_rounded),
    (DeliveryStatus.delivered, 'Delivered', Icons.local_gas_station_rounded),
  ];

  @override
  Widget build(BuildContext context) {
    final current = DeliveryStatus.steps.indexOf(status);
    final isCancelled = status == DeliveryStatus.cancelled || status == DeliveryStatus.failed;

    if (isCancelled) {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
        decoration: BoxDecoration(
          color: const Color(0xFFEF4444).withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: const Color(0xFFEF4444).withValues(alpha: 0.3)),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cancel_rounded, color: Color(0xFFEF4444), size: 18),
            const SizedBox(width: 8),
            Text(
              status == DeliveryStatus.cancelled ? 'Delivery Cancelled' : 'Delivery Failed',
              style: const TextStyle(color: Color(0xFFDC2626), fontWeight: FontWeight.bold, fontSize: 13),
            ),
          ],
        ),
      );
    }

    return Column(
      children: [
        Row(
          children: [
            for (var i = 0; i < _stepDetails.length; i++) ...[
              // Step Node
              _StepNode(
                isCompleted: i <= current,
                isCurrent: i == current,
                icon: _stepDetails[i].$3,
                label: _stepDetails[i].$2,
              ),
              // Connecting Line
              if (i < _stepDetails.length - 1)
                Expanded(
                  child: Container(
                    height: 4,
                    margin: const EdgeInsets.only(bottom: 22, left: 4, right: 4),
                    decoration: BoxDecoration(
                      gradient: i < current
                          ? const LinearGradient(
                              colors: [Color(0xFF004D3D), Color(0xFF10B981)],
                            )
                          : null,
                      color: i >= current ? Colors.grey.withValues(alpha: 0.2) : null,
                      borderRadius: BorderRadius.circular(2),
                    ),
                  ),
                ),
            ],
          ],
        ),
      ],
    );
  }
}

class _StepNode extends StatelessWidget {
  const _StepNode({
    required this.isCompleted,
    required this.isCurrent,
    required this.icon,
    required this.label,
  });

  final bool isCompleted;
  final bool isCurrent;
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    const activeColor = Color(0xFF004D3D);

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 34,
          height: 34,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: isCompleted ? activeColor : Colors.grey.withValues(alpha: 0.15),
            boxShadow: isCurrent
                ? [
                    BoxShadow(
                      color: activeColor.withValues(alpha: 0.4),
                      blurRadius: 10,
                      spreadRadius: 2,
                    ),
                  ]
                : null,
            border: Border.all(
              color: isCompleted ? Colors.white : Colors.grey.withValues(alpha: 0.3),
              width: 2,
            ),
          ),
          child: Icon(
            icon,
            size: 16,
            color: isCompleted ? Colors.white : Colors.grey,
          ),
        ),
        const SizedBox(height: 6),
        Text(
          label,
          style: TextStyle(
            fontSize: 10,
            fontWeight: isCurrent ? FontWeight.w800 : FontWeight.w600,
            color: isCompleted ? activeColor : Colors.grey,
          ),
        ),
      ],
    );
  }
}
