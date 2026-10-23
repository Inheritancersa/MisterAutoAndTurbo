import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/quotation.dart';
import '../models/vehicle.dart';
import '../routes/route_manager.dart';
import '../theme.dart';
import '../utils/helpers.dart';
import '../viewmodels/workshop_viewmodel.dart';
import '../widgets/common.dart';

/// Quotation Phone (29:75) with the Accepted (29:143) and Rejected (36:191)
/// states rendered in-place once the customer decides.
class QuotationView extends StatelessWidget {
  const QuotationView({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<WorkshopViewModel>(
      builder: (context, workshop, _) {
        final q = workshop.quotation;

        // Nothing issued yet: blank table until reception sends a quotation.
        if (q == null) {
          return AppScreen(
      onRefresh: () => context.read<WorkshopViewModel>().refresh(),
            children: [
              Text('Quotation', style: AppText.plex(24, c: Colors.black)),
              const SizedBox(height: 14),
              const _QuotationTable(lines: [], total: null),
              const SizedBox(height: 24),
              Text('Waiting for reception to send your quotation.',
                  textAlign: TextAlign.center,
                  style: AppText.plex(15, w: FontWeight.w500, c: AppColors.label)),
            ],
          );
        }

        final decided = q.status != QuotationStatus.pending;
        final lineStyle = AppText.plex(16, w: FontWeight.w600, c: Colors.black);
        return AppScreen(
          children: [
            Text('Quotation', style: AppText.plex(24, c: Colors.black)),
            const SizedBox(height: 14),
            _QuotationDetails(
              quotation: q,
              vehicle: workshop.vehicleForQuotation(q),
            ),
            const SizedBox(height: 14),
            _QuotationTable(lines: q.lines, total: q.total),
            const SizedBox(height: 38),
            if (q.customerSuppliedSaving > 0)
              InfoCard(
                color: AppColors.noteBg,
                borderColor: AppColors.noteBorder,
                padding: const EdgeInsets.all(16),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text('If You Supply The Part.', style: lineStyle),
                  const SizedBox(height: 16),
                  Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
                    Text('Save ${rand(q.customerSuppliedSaving).replaceAll('.', '')}.',
                        style: lineStyle),
                    Text(rand(q.totalIfCustomerSupplies), style: lineStyle),
                  ]),
                ]),
              ),
            const SizedBox(height: 36),
            if (!decided) ...[
              WideButton('Accept Quotation',
                  green: true, onPressed: () async { final e = await workshop.decideQuotation(true); if (e != null && context.mounted) snack(context, e); }),
              const SizedBox(height: 36),
              WideButton('Reject Quotation',
                  outlined: true, onPressed: () async { final e = await workshop.decideQuotation(false); if (e != null && context.mounted) snack(context, e); }),
            ] else
              _DecisionResult(accepted: q.status == QuotationStatus.accepted),
            if (q.status == QuotationStatus.accepted) ...[
              const SizedBox(height: 24),
              WideButton('Proof Of Payment',
                  onPressed: () =>
                      Navigator.pushNamed(context, RouteManager.proofOfPayment)),
            ],
          ],
        );
      },
    );
  }
}

/// Vehicle, registration plate, job card and total for this quotation.
class _QuotationDetails extends StatelessWidget {
  final Quotation quotation;
  final Vehicle? vehicle;
  const _QuotationDetails({required this.quotation, required this.vehicle});

  @override
  Widget build(BuildContext context) {
    final label = AppText.plex(14, w: FontWeight.w500, c: AppColors.label);
    final value = AppText.inter(18, w: FontWeight.w700, c: Colors.black);
    Widget row(String l, String v) => Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(l, style: label),
            Text(v, style: value),
          ]),
        );
    final hasPlate = vehicle != null && vehicle!.plate.isNotEmpty;
    return InfoCard(
      color: AppColors.surface,
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        row('Vehicle', vehicle?.displayName.trim() ?? 'Not available'),
        row('Registration', hasPlate ? vehicle!.plate : '-'),
        row('Job Card', quotation.jobId.isEmpty ? quotation.id : quotation.jobId),
        row('Total Amount', rand(quotation.total).replaceAll(RegExp(r'\.$'), '')),
      ]),
    );
  }
}

/// The Item / Amount table. An empty [lines] list draws just the header.
class _QuotationTable extends StatelessWidget {
  final List<QuotationLine> lines;
  final double? total;
  const _QuotationTable({required this.lines, required this.total});

  @override
  Widget build(BuildContext context) {
    final lineStyle = AppText.plex(16, w: FontWeight.w600, c: Colors.black);
    return InfoCard(
      color: AppColors.surface,
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 12),
      child: Column(children: [
        Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
          Text('Item', style: AppText.plex(24, c: Colors.black)),
          Text('Amount', style: AppText.plex(24, c: Colors.black)),
        ]),
        for (final l in lines)
          Padding(
            padding: const EdgeInsets.only(top: 4),
            child: Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
              Text(l.item, style: lineStyle),
              Text(rand(l.amount), style: lineStyle),
            ]),
          ),
        if (total != null) ...[
          const Divider(height: 20),
          Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
            Text('Total', style: AppText.plex(18, w: FontWeight.w700, c: Colors.black)),
            Text(rand(total!), style: AppText.plex(18, w: FontWeight.w700, c: Colors.black)),
          ]),
        ],
      ]),
    );
  }
}

/// Shown after the customer accepts or rejects the quotation.
class _DecisionResult extends StatelessWidget {
  final bool accepted;
  const _DecisionResult({required this.accepted});

  @override
  Widget build(BuildContext context) => InfoCard(
        color: accepted ? AppColors.successBg : AppColors.surface,
        borderColor: accepted ? AppColors.successBorder : AppColors.border,
        padding: const EdgeInsets.all(20),
        child: Column(children: [
          Icon(accepted ? Icons.check_circle : Icons.cancel,
              size: 44, color: accepted ? AppColors.done : Colors.red),
          const SizedBox(height: 8),
          Text(accepted ? 'Quotation Accepted' : 'Quotation Rejected',
              style: AppText.plex(20, c: Colors.black)),
          const SizedBox(height: 4),
          Text(
              accepted
                  ? 'Thank you. The workshop will now start the repair.'
                  : 'The workshop has been notified and will contact you.',
              textAlign: TextAlign.center,
              style: AppText.plex(14, w: FontWeight.w500, c: AppColors.label)),
        ]),
      );
}
