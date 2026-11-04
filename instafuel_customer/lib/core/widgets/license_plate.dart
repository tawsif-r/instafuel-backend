import 'package:flutter/material.dart';

/// Number plate look-alike, so the customer sees exactly what the driver will match.
class LicensePlate extends StatelessWidget {
  const LicensePlate(this.registrationNumber, {super.key, this.compact = false});

  final String registrationNumber;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: compact ? 8 : 12, vertical: compact ? 3 : 6),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: Colors.black87, width: 1.5),
      ),
      child: Text(
        registrationNumber,
        style: TextStyle(
          color: Colors.black87,
          fontWeight: FontWeight.w700,
          letterSpacing: 0.8,
          fontSize: compact ? 11 : 14,
        ),
      ),
    );
  }
}
