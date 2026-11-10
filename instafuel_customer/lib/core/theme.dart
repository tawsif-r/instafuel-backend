// Deploy-grade Nordic Pine & Aurora Mint Palette
// Signature luxury green: Deep Nordic Pine (0xFF00382B), Aurora Mint (0xFF00E599),
// and Warm Eucalyptus / Sage tint surfaces.

import 'package:flutter/material.dart';

class AppTheme {
  AppTheme._();

  // Signature Nordic Pine & Aurora Mint Palette
  static const seed = Color(0xFF00382B);
  static const primaryGreen = Color(0xFF004D3D);       // Deep Nordic Pine
  static const pineDeep = Color(0xFF002E23);           // Imperial Pine / Dark Obsidian
  static const mintAccent = Color(0xFF00E599);         // Aurora Mint (Electric, crisp, beautiful)
  static const jadeLight = Color(0xFF10B981);          // Vibrant Jade
  static const eucalyptusSage = Color(0xFFD1E7DD);     // Soft Eucalyptus border
  static const lightBg = Color(0xFFF2F7F4);            // Crisp Mint Mist / Celadon background
  static const darkBg = Color(0xFF071410);             // Midnight Forest dark mode
  static const darkSurface = Color(0xFF0C1F1A);        // Elevated Pine surface

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
      primary: isDark ? mintAccent : primaryGreen,
      secondary: const Color(0xFF0F766E),
      surface: isDark ? darkSurface : Colors.white,
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
          color: isDark ? Colors.white : pineDeep,
          fontSize: 20,
          fontWeight: FontWeight.w800,
          letterSpacing: -0.4,
        ),
        iconTheme: IconThemeData(color: isDark ? Colors.white : pineDeep),
      ),
      cardTheme: CardThemeData(
        elevation: isDark ? 0 : 2,
        shadowColor: primaryGreen.withValues(alpha: 0.08),
        margin: EdgeInsets.zero,
        color: isDark ? darkSurface : Colors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radius),
          side: BorderSide(
            color: isDark ? const Color(0xFF16382E) : eucalyptusSage,
            width: 1.2,
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: isDark ? darkSurface : Colors.white,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(16)),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(16),
          borderSide: BorderSide(
            color: isDark ? const Color(0xFF16382E) : const Color(0xFFC3DFD3),
          ),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(16),
          borderSide: const BorderSide(color: primaryGreen, width: 2),
        ),
        labelStyle: TextStyle(
          color: isDark ? Colors.grey[400] : const Color(0xFF285043),
          fontWeight: FontWeight.w500,
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: primaryGreen,
          foregroundColor: Colors.white,
          minimumSize: const Size(64, 54),
          shape: rounded,
          elevation: 2.5,
          shadowColor: primaryGreen.withValues(alpha: 0.35),
          textStyle: const TextStyle(fontWeight: FontWeight.w800, fontSize: 15.5, letterSpacing: -0.2),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size(64, 54),
          shape: rounded,
          side: const BorderSide(color: primaryGreen, width: 1.5),
          foregroundColor: primaryGreen,
          textStyle: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
        ),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        backgroundColor: pineDeep,
        contentTextStyle: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600),
      ),
      listTileTheme: const ListTileThemeData(
        contentPadding: EdgeInsets.symmetric(horizontal: gap16),
      ),
    );
  }
}
