import '../utils/helpers.dart';

class NotificationItem {
  final String title;
  final String body;
  /// When the message was sent / received (local time).
  final DateTime? at;
  final bool highlighted;
  const NotificationItem(this.title, this.body, {this.at, this.highlighted = false});

  /// Display text such as "22 Oct 2026, 14:05".
  String? get when => at == null ? null : fmtDateTime(at!);
}
