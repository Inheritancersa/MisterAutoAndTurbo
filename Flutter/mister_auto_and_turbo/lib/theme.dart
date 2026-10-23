import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

/// Design tokens taken from the Figma "customer-proto" file.
class AppColors {
  static const brand = Color(0xFF2B5C8F); // splash-header / primary buttons
  static const link = Color(0xFF1A62BD); // blue labels
  static const surface = Color(0xFFF9FAFB); // input-box fill
  static const border = Color(0xFFE2E0D8);
  static const ink = Color(0xFF17201F);
  static const label = Color(0xFF5C6775);
  static const warn = Color(0xFFFFB20B); // Forgot password
  static const loginDisabled = Color(0xFFBECCDA);
  static const successBg = Color(0xFFE1F5EE);
  static const successBorder = Color(0xFF0F6E56);
  static const noteBg = Color(0xFFFAEEDA);
  static const noteBorder = Color(0xFF854F0B);
  static const done = Color(0xFF00B518);
}

class AppText {
  static TextStyle inter(double size, {FontWeight w = FontWeight.w600, Color c = AppColors.ink}) =>
      GoogleFonts.inter(fontSize: size, fontWeight: w, color: c);
  static TextStyle plex(double size, {FontWeight w = FontWeight.w700, Color c = AppColors.link}) =>
      GoogleFonts.ibmPlexSans(fontSize: size, fontWeight: w, color: c);
}

ThemeData buildTheme() => ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(seedColor: AppColors.brand),
      scaffoldBackgroundColor: Colors.white,
      textTheme: GoogleFonts.interTextTheme(),
    );
