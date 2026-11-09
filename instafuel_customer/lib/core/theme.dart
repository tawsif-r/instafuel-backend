// Deploy-grade Emerald Green Theme (FuelBuddy / Fuel Go inspired)
// Primary: Emerald Green (0xFF00A86B / 0xFF059669)
// Accent: Mint (0xFF10B981) & Dark Forest / Obsidian Slate (0xFF0A192F / 0xFF0F172A)

import 'package:flutter/material.dart';

class AppTheme {
  AppTheme._();

  // Emerald fuel green
  static const seed = Color(0xFF00A86B);
  static const primaryGreen = Color(0xFF059669);
  static const darkForest = Color(0xFF064E3B);
  static const mintAccent = Color(0xFF10B981);
  static const lightBg = Color(0xFFF6FBF7);
  static const darkBg = Color(0xFF0B1320);

  // Spacing scale
  static const gap4 = 4.0;
  static const gap6 = 6.0;
  static const gap8 = 8.0;
  static const gap10 = 10.0;
  static const gap12 = 12.0;
  static const gap14 = 14.0;
  static const gap16 = 16.0;
  static const gap20 = 20.0;
  static const gap24 = 24.0;
  static const gap28 = 28.0;
  static const gap32 = 32.0;

  static const radius = 20.0;

  static ThemeData light() => _build(Brightness.light);
  static ThemeData dark() => _build(Brightness.dark);

  static ThemeData _build(Brightness brightness) {
    final isDark = brightness == Brightness.dark;
    final scheme = ColorScheme.fromSeed(
      seedColor: seed,
      brightness: brightness,
      primary: isDark ? const Color(0xFF34D399) : const Color(0xFF059669),
      secondary: const Color(0xFF0D9488),
      surface: isDark ? darkBg : Colors.white,
    );
    final rounded = RoundedRectangleBorder(borderRadius: BorderRadius.circular(16));

    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: isDark ? darkBg : lightBg,
      appBarTheme: AppBarTheme(
        centerTitle: false,
        backgroundColor: isDark ? darkBg : lightBg,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        titleTextStyle: TextStyle(
          color: isDark ? Colors.white : const Color(0xFF0F172A),
          fontSize: 19,
          fontWeight: FontWeight.w800,
          letterSpacing: -0.4,
        ),
        iconTheme: IconThemeData(color: isDark ? Colors.white : const Color(0xFF0F172A)),
      ),
      cardTheme: CardThemeData(
        elevation: isDark ? 0 : 2,
        shadowColor: const Color(0xFF059669).withValues(alpha: 0.06),
        margin: EdgeInsets.zero,
        color: isDark ? const Color(0xFF131F33) : Colors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radius),
          side: BorderSide(
            color: isDark ? const Color(0xFF1E2E48) : const Color(0xFFE2EBE5),
            width: 1.2,
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: isDark ? const Color(0xFF131F33) : Colors.white,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(16)),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(16),
          borderSide: BorderSide(
            color: isDark ? const Color(0xFF1E2E48) : const Color(0xFFD1E0D7),
          ),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(16),
          borderSide: const BorderSide(color: Color(0xFF059669), width: 2),
        ),
        labelStyle: TextStyle(
          color: isDark ? Colors.grey[400] : const Color(0xFF475569),
          fontWeight: FontWeight.w500,
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: const Color(0xFF059669),
          foregroundColor: Colors.white,
          minimumSize: const Size(64, 54),
          shape: rounded,
          elevation: 2,
          shadowColor: const Color(0xFF059669).withValues(alpha: 0.35),
          textStyle: const TextStyle(fontWeight: FontWeight.w800, fontSize: 15.5, letterSpacing: -0.2),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size(64, 54),
          shape: rounded,
          side: const BorderSide(color: Color(0xFF059669), width: 1.5),
          foregroundColor: const Color(0xFF059669),
          textStyle: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
        ),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        backgroundColor: const Color(0xFF0F172A),
        contentTextStyle: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600),
      ),
      listTileTheme: const ListTileThemeData(
        contentPadding: EdgeInsets.symmetric(horizontal: gap16),
      ),
    );
  }
}
