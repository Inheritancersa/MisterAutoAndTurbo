import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

// ============================================================
// SHARED UI HELPERS
// Small, stateless helper functions used by many views.
// ============================================================

/// Parses a timestamp from the database. Values without a timezone are
/// treated as UTC (how the staff app stores them) and converted to the
/// phone's local time.
DateTime? parseDbTime(Object? raw) {
  final s = raw?.toString().trim() ?? '';
  if (s.isEmpty) return null;
  final hasZone = RegExp(r'(Z|[+-]\d{2}(:?\d{2})?)$').hasMatch(s);
  final parsed = DateTime.tryParse(hasZone ? s : '${s.replaceFirst(' ', 'T')}Z');
  return parsed?.toLocal();
}

/// Formats a date and time, e.g. "22 Oct 2026, 14:05".
String fmtDateTime(DateTime d) {
  const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  String two(int n) => n.toString().padLeft(2, '0');
  return '${d.day} ${months[d.month - 1]} ${d.year}, ${two(d.hour)}:${two(d.minute)}';
}

/// Shows a [SnackBar] with [msg] on the nearest [Scaffold].
void snack(BuildContext c, String msg) =>
    ScaffoldMessenger.of(c).showSnackBar(SnackBar(content: Text(msg)));

/// Form validator: the field must not be empty.
String? requiredField(String? v) =>
    (v == null || v.trim().isEmpty) ? 'Required' : null;

/// Form validator: the field must look like an email address.
String? emailField(String? v) =>
    (v == null || !v.contains('@')) ? 'Enter a valid email' : null;

/// Phone: digits only, at least 10 digits (South African style).
String? phoneField(String? v) {
  if (v == null || v.trim().isEmpty) return 'Required';
  final trimmed = v.trim();
  if (RegExp(r'[^0-9]').hasMatch(trimmed)) {
    return 'Numbers only — no letters or special characters';
  }
  if (trimmed.length < 10) return 'Enter a valid phone number (at least 10 digits)';
  if (trimmed.length > 15) return 'Phone number is too long';
  return null;
}

/// Digits-only (mileage, year, etc.).
String? digitsField(String? v, {int? min, int? max, String label = 'Value'}) {
  if (v == null || v.trim().isEmpty) return 'Required';
  if (RegExp(r'[^0-9]').hasMatch(v.trim())) {
    return 'Numbers only — no letters or special characters';
  }
  final n = int.tryParse(v.trim());
  if (n == null) return 'Enter a valid number';
  if (min != null && n < min) return '$label must be at least $min';
  if (max != null && n > max) return '$label must be at most $max';
  return null;
}

/// Blocks letters and special characters; allows digits only.
final digitsOnlyFormatter = FilteringTextInputFormatter.digitsOnly;

const _months = [
  'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
];

/// Formats a date, for example: 13 Oct 2026.
String fmtDate(DateTime date) =>
    '${date.day} ${_months[date.month - 1]} ${date.year}';

/// Formats a byte count, for example: 512 B, 40 KB, 2.50 MB.
String fmtSize(int bytes) {
  if (bytes < 1024) return '$bytes B';
  if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(0)} KB';
  return '${(bytes / 1024 / 1024).toStringAsFixed(2)} MB';
}
