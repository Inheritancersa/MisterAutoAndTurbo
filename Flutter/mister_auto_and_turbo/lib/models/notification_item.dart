import '../utils/helpers.dart';
/// Single notification entry shown on the Notifications screen.
///
/// Notifications are typically generated from workshop events
/// (quotation sent, job stage changed, etc.) and displayed in a list.
class NotificationItem {
  final String title;
  final String body;
  /// When the message was sent / received (local time).
  final DateTime? at;
  final bool highlighted;
  const NotificationItem(this.title, this.body, {this.at, this.highlighted = false});

  /// True for quotation messages (e.g. "Quotation Sent"); tapping opens the quotation.
  bool get isQuotation =>
      '$title $body'.toLowerCase().contains('quot');

  /// Display text such as "22 Oct 2026, 14:05".
  String? get when => at == null ? null : fmtDateTime(at!);
}
