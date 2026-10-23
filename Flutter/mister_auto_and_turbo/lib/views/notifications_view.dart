import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/notification_item.dart';
import '../theme.dart';
import '../viewmodels/workshop_viewmodel.dart';
import '../widgets/common.dart';

/// Notifications (39:167)
class NotificationsView extends StatelessWidget {
  const NotificationsView({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<WorkshopViewModel>(
      builder: (context, workshop, _) => AppScreen(
      onRefresh: () => context.read<WorkshopViewModel>().refresh(),
        title: 'Notifications.',
        children: [
          for (final n in workshop.notifications) ...[
            _NotificationCard(item: n),
            const SizedBox(height: 26),
          ],
        ],
      ),
    );
  }
}

class _NotificationCard extends StatelessWidget {
  final NotificationItem item;
  const _NotificationCard({required this.item});

  @override
  Widget build(BuildContext context) {
    final textColor = item.highlighted ? const Color(0xFFFFFAFA) : Colors.black;
    return InfoCard(
      color: item.highlighted ? AppColors.brand : AppColors.surface,
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(item.title, style: AppText.inter(26, c: textColor)),
        const SizedBox(height: 4),
        Text(item.body, style: AppText.inter(20, c: textColor)),
        if (item.when != null) ...[
          const SizedBox(height: 6),
          Row(children: [
            Icon(Icons.access_time, size: 16, color: textColor),
            const SizedBox(width: 6),
            Text(item.when!, style: AppText.inter(15, w: FontWeight.w500, c: textColor)),
          ]),
        ],
      ]),
    );
  }
}
