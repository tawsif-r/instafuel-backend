import 'package:flutter/material.dart';

import '../theme.dart';

/// A card with an optional title row. Used for most grouped content.
class SectionCard extends StatelessWidget {
  const SectionCard({
    super.key,
    required this.child,
    this.title,
    this.trailing,
    this.padding = const EdgeInsets.all(AppTheme.gap16),
  });

  final Widget child;
  final String? title;
  final Widget? trailing;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: padding,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (title != null) ...[
              Row(
                children: [
                  Expanded(child: Text(title!, style: Theme.of(context).textTheme.titleMedium)),
                  ?trailing, // only added when not null
                ],
              ),
              const SizedBox(height: AppTheme.gap12),
            ],
            child,
          ],
        ),
      ),
    );
  }
}
