import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../theme.dart';
import '../utils/helpers.dart';
import '../viewmodels/workshop_viewmodel.dart';
import '../widgets/common.dart';

/// Repair History (39:116)
class HistoryView extends StatelessWidget {
  const HistoryView({super.key});

  @override
  Widget build(BuildContext context) {
    const white = Color(0xFFFFFAFA);
    return Consumer<WorkshopViewModel>(
      builder: (context, workshop, _) => AppScreen(
        onRefresh: () => context.read<WorkshopViewModel>().refresh(),
        title: 'Repair History.',
        children: [
          if (workshop.history.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 24),
              child: Center(
                child: Text(
                  'No repair history yet.\nWhen staff save a diagnosis or notes, they appear here.',
                  textAlign: TextAlign.center,
                  style: AppText.plex(16, c: AppColors.label),
                ),
              ),
            ),
          for (final h in workshop.history) ...[
            InfoCard(
              color: AppColors.brand,
              minHeight: 126,
              padding: const EdgeInsets.fromLTRB(12, 8, 12, 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(h.title, style: AppText.inter(26, c: white)),
                  const SizedBox(height: 6),
                  Text(h.subtitle, style: AppText.inter(22, c: white)),
                  if (h.date != null && h.cost != null) ...[
                    const SizedBox(height: 6),
                    Text(
                      '${fmtDate(h.date!)} · R${h.cost!.toStringAsFixed(0)}',
                      style: AppText.inter(22, c: white),
                    ),
                  ],
                  if (h.hasDiagnosis) ...[
                    const SizedBox(height: 10),
                    Text('Diagnosis',
                        style: AppText.plex(14, w: FontWeight.w600, c: white)),
                    const SizedBox(height: 4),
                    Text(h.diagnosis,
                        style: AppText.inter(16, w: FontWeight.w500, c: white)),
                  ],
                  if (h.hasRepairNotes) ...[
                    const SizedBox(height: 10),
                    Text('Repair Notes',
                        style: AppText.plex(14, w: FontWeight.w600, c: white)),
                    const SizedBox(height: 4),
                    Text(h.repairNotes,
                        style: AppText.inter(16, w: FontWeight.w500, c: white)),
                  ],
                ],
              ),
            ),
            const SizedBox(height: 32),
          ],
        ],
      ),
    );
  }
}
