import 'package:flutter/material.dart';

/// Keeps content readable on wide screens (web/tablet) by capping its width.
/// By default [heightFactor] is 1.0 so that when placed inside constrained containers
/// (such as Scaffold bottomNavigationBar or bars), it does not vertically expand to fill the entire height.
class ResponsiveCenter extends StatelessWidget {
  const ResponsiveCenter({
    super.key,
    required this.child,
    this.maxWidth = 640,
    this.heightFactor = 1.0,
  });

  final Widget child;
  final double maxWidth;
  final double? heightFactor;

  @override
  Widget build(BuildContext context) => Align(
        alignment: Alignment.topCenter,
        heightFactor: heightFactor,
        child: ConstrainedBox(constraints: BoxConstraints(maxWidth: maxWidth), child: child),
      );
}


