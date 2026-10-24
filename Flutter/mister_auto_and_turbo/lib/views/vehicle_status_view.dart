import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/repair_job.dart';
import '../theme.dart';
import '../utils/helpers.dart';
import '../viewmodels/vehicle_status_viewmodel.dart';
import '../widgets/common.dart';

/// Current Status / Repair Progress screen.
///
/// Displays the progress of a vehicle that has a repair job. Newly registered
/// vehicles can also open this screen, even if a repair job has not been
/// created yet. This is a dynamic route: the vehicle arrives as a route
/// argument and is held by [VehicleStatusViewModel].
class VehicleStatusView extends StatelessWidget {
  const VehicleStatusView({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<VehicleStatusViewModel>(
      builder: (context, vm, _) {
        final job = vm.job;
        return AppScreen(
        onRefresh: () async { await vm.refresh(); },
          title: 'Current Status.',
          children: [
            // Vehicle details
            InfoCard(
              color: AppColors.brand,
              padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    vm.vehicle.displayName,
                    style: AppText.inter(30, c: const Color(0xFFFFFAFA)),
                  ),
                  const SizedBox(height: 8),
                  Text(vm.summary, style: AppText.inter(20, c: const Color(0xFFFFFAFA))),
                  if (vm.jobCardId != null)
                    Text('Job Card ID: ${vm.jobCardId}',
                        style: AppText.inter(16, c: const Color(0xFFFFFAFA))),
                  if (vm.vehicle.mileage > 0)
                    Text('Mileage: ${vm.vehicle.mileage} km',
                        style: AppText.inter(16, c: const Color(0xFFFFFAFA))),
                ],
              ),
            ),

            const SizedBox(height: 16),

            Text('Repair Progress.', style: AppText.inter(24)),

            const SizedBox(height: 14),

            // No repair job message
            if (job == null) ...[
              InfoCard(
                child: Text(
                  'Your vehicle is registered successfully. '
                  'A repair job has not been created yet. '
                  'Your progress tracker will reflect the repair '
                  'stages once a repair job is available.',
                  style: AppText.plex(15, c: AppColors.label),
                ),
              ),
              const SizedBox(height: 16),
            ],

            // Diagnosis & repair notes from staff (ASP.NET JobCards)
            if (job != null && (job.hasDiagnosis || job.hasRepairNotes)) ...[
              if (job.hasDiagnosis) ...[
                Text('Diagnosis', style: AppText.plex(16, c: AppColors.label)),
                const SizedBox(height: 8),
                InputBox(
                  child: Text(
                    job.diagnosis,
                    style: AppText.inter(16, w: FontWeight.w500, c: Colors.black),
                  ),
                ),
                const SizedBox(height: 16),
              ],
              if (job.hasRepairNotes) ...[
                Text('Repair Notes', style: AppText.plex(16, c: AppColors.label)),
                const SizedBox(height: 8),
                InputBox(
                  child: Text(
                    job.repairNotes,
                    style: AppText.inter(16, w: FontWeight.w500, c: Colors.black),
                  ),
                ),
                const SizedBox(height: 16),
              ],
            ],

            // Repair stages. Existing jobs display their saved progress;
            // newly registered vehicles display every stage as pending.
            for (final stage in JobStage.values) ...[
              _StageRow(stage: stage, done: job != null && job.stageDone(stage)),
              const SizedBox(height: 25),
            ],

            // Refresh status from Supabase
            InputBox(
              height: 52,
              onTap: vm.isRefreshing
                  ? null
                  : () async {
                      final msg = await vm.refresh();
                      if (context.mounted) snack(context, msg);
                    },
              child: Center(
                child: vm.isRefreshing
                    ? const SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text('Refresh Status', style: AppText.plex(16)),
              ),
            ),
          ],
        );
      },
    );
  }
}

/// One row of the progress tracker: a tick when [done], an empty circle if not.
class _StageRow extends StatelessWidget {
  final JobStage stage;
  final bool done;
  const _StageRow({required this.stage, required this.done});

  @override
  Widget build(BuildContext context) => InputBox(
        height: 52,
        child: Row(
          children: [
            done
                ? const Icon(Icons.check_circle, color: AppColors.done, size: 30)
                : const Icon(Icons.radio_button_unchecked, color: Colors.black, size: 30),
            Expanded(
              child: Center(child: Text(stage.label, style: AppText.plex(16))),
            ),
            const SizedBox(width: 30),
          ],
        ),
      );
}