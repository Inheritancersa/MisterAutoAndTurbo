import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../theme.dart';
import '../utils/helpers.dart';
import '../viewmodels/complaint_viewmodel.dart';
import '../widgets/common.dart';

/// Complaints Phone (75:196) + Complaints Accepted (156:85) confirmation.
///
/// Stateful only to own the form key and the description controller. The
/// chosen type and the "submitted" flag live in [ComplaintViewModel].
class ComplaintsView extends StatefulWidget {
  const ComplaintsView({super.key});
  @override
  State<ComplaintsView> createState() => _ComplaintsViewState();
}

class _ComplaintsViewState extends State<ComplaintsView> {
  final _form = GlobalKey<FormState>();
  final _desc = TextEditingController();

  @override
  void dispose() {
    _desc.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final label = AppText.inter(20);
    return Consumer<ComplaintViewModel>(
      builder: (context, vm, _) {
        if (vm.sent) return const _SubmittedScreen();

        return AppScreen(
          title: 'Complaints.',
          children: [
            Form(
              key: _form,
              child: Column(children: [
                Padding(
                  padding: const EdgeInsets.only(bottom: 14),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text('Complaint Type', style: label),
                    const SizedBox(height: 8),
                    DropdownButtonFormField<String>(
                      value: vm.type,
                      isExpanded: true,
                      hint: Text('Select complaint type',
                          style: AppText.inter(16, w: FontWeight.w500, c: AppColors.label)),
                      style: AppText.inter(16, w: FontWeight.w500),
                      icon: const Icon(Icons.keyboard_arrow_down, color: Colors.black),
                      items: [
                        for (final t in ComplaintViewModel.types)
                          DropdownMenuItem(value: t, child: Text(t)),
                      ],
                      onChanged: vm.selectType,
                      validator: (v) => v == null ? 'Please select a complaint type' : null,
                      decoration: fieldDecoration(),
                    ),
                  ]),
                ),
                Padding(
                  padding: const EdgeInsets.only(bottom: 14),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text('Vehicle (optional)', style: label),
                    const SizedBox(height: 8),
                    DropdownButtonFormField<String?>(
                      value: vm.vehicleId,
                      isExpanded: true,
                      hint: Text('Select vehicle',
                          style: AppText.inter(16, w: FontWeight.w500, c: AppColors.label)),
                      style: AppText.inter(16, w: FontWeight.w500, c: Colors.black),
                      icon: const Icon(Icons.keyboard_arrow_down, color: Colors.black),
                      items: [
                        const DropdownMenuItem<String?>(value: null, child: Text('None')),
                        for (final v in vm.vehicles)
                          DropdownMenuItem<String?>(
                              value: v.id, child: Text('${v.displayName} (${v.plate})')),
                      ],
                      onChanged: vm.selectVehicle,
                      decoration: fieldDecoration(),
                    ),
                  ]),
                ),
                Padding(
                  padding: const EdgeInsets.only(bottom: 14),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text('Job (optional)', style: label),
                    const SizedBox(height: 8),
                    DropdownButtonFormField<String?>(
                      value: vm.jobId,
                      isExpanded: true,
                      hint: Text('Select job',
                          style: AppText.inter(16, w: FontWeight.w500, c: AppColors.label)),
                      style: AppText.inter(16, w: FontWeight.w500, c: Colors.black),
                      icon: const Icon(Icons.keyboard_arrow_down, color: Colors.black),
                      items: [
                        const DropdownMenuItem<String?>(value: null, child: Text('None')),
                        for (final j in vm.jobs)
                          DropdownMenuItem<String?>(
                              value: j.id,
                              child: Text('${j.title} · #${j.id}', overflow: TextOverflow.ellipsis)),
                      ],
                      onChanged: vm.selectJob,
                      decoration: fieldDecoration(),
                    ),
                  ]),
                ),
                LabeledField(
                  label: 'Complaint Description',
                  labelStyle: label,
                  controller: _desc,
                  maxLines: 5,
                  validator: requiredField,
                ),
              ]),
            ),
            const SizedBox(height: 20),
            WideButton('Submit Complaint', onPressed: () async {
              if (!_form.currentState!.validate()) return;
              final err = await vm.submit(description: _desc.text.trim());
              if (err != null && context.mounted) snack(context, err);
            }),
          ],
        );
      },
    );
  }
}

/// Complaint accepted confirmation.
class _SubmittedScreen extends StatelessWidget {
  const _SubmittedScreen();

  @override
  Widget build(BuildContext context) => AppScreen(
        title: 'Complaints.',
        children: [
          InfoCard(
            color: AppColors.successBg,
            borderColor: AppColors.successBorder,
            padding: const EdgeInsets.all(20),
            child: Column(children: [
              const Icon(Icons.check_circle, size: 48, color: AppColors.done),
              const SizedBox(height: 8),
              Text('Complaint Submitted', style: AppText.inter(22)),
              const SizedBox(height: 6),
              Text('Thank you. Reception will review it and contact you.',
                  textAlign: TextAlign.center,
                  style: AppText.plex(15, w: FontWeight.w500, c: AppColors.label)),
            ]),
          ),
        ],
      );
}