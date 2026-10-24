import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/quotation.dart';
import '../models/vehicle.dart';
import '../theme.dart';
import '../utils/helpers.dart';
import '../viewmodels/auth_viewmodel.dart';
import '../viewmodels/proof_of_payment_viewmodel.dart';
import '../viewmodels/workshop_viewmodel.dart';
import '../widgets/common.dart';

/// Proof Of Payment (86:35) + Proof Of Pay Accepted (156:22).
/// Supports multiple documents with a combined maximum size of 10 MB.
///
/// A StatelessWidget: the selected files, the "picking" flag and the "done"
/// flag all live in [ProofOfPaymentViewModel].
class ProofOfPaymentView extends StatelessWidget {
  const ProofOfPaymentView({super.key});

  @override
  Widget build(BuildContext context) {
    // Only the "done" flag decides which screen is drawn.
    return Selector<ProofOfPaymentViewModel, bool>(
      selector: (_, vm) => vm.done,
      builder: (context, done, _) =>
          done ? const _UploadedScreen() : const _UploadScreen(),
    );
  }
}

/// Proof Of Pay Accepted.
class _UploadedScreen extends StatelessWidget {
  const _UploadedScreen();

  @override
  Widget build(BuildContext context) => AppScreen(
        greeting: 'Proof Of Pay',
        children: [
          InfoCard(
            color: AppColors.successBg,
            borderColor: AppColors.successBorder,
            padding: const EdgeInsets.all(20),
            child: Column(
              children: [
                const Icon(Icons.check_circle, size: 48, color: AppColors.done),
                const SizedBox(height: 8),
                Text('Proof Of Payment Uploaded', style: AppText.inter(20)),
                const SizedBox(height: 6),
                Text(
                  'The workshop will confirm your payment shortly.',
                  textAlign: TextAlign.center,
                  style: AppText.plex(15, w: FontWeight.w500, c: AppColors.label),
                ),
              ],
            ),
          ),
        ],
      );
}

/// Proof Of Payment (86:35): the upload form.
class _UploadScreen extends StatelessWidget {
  const _UploadScreen();

  Future<void> _pick(BuildContext context) async {
    final message = await context.read<ProofOfPaymentViewModel>().pickFiles();
    if (message != null && context.mounted) snack(context, message);
  }

  @override
  Widget build(BuildContext context) {
    final vm = context.read<ProofOfPaymentViewModel>();
    final medium = AppText.inter(18, w: FontWeight.w500, c: Colors.black);

    return AppScreen(
      greeting: 'Proof Of Pay',
      children: [
        const _QuotationSummary(),

        const SizedBox(height: 22),

        Text('Upload Payment Document', style: AppText.inter(20, c: Colors.black)),
        const SizedBox(height: 4),
        Text('Upload your proof of payment below.', style: medium),
        const SizedBox(height: 4),
        Text(
          'PDF, JPG or PNG • Maximum combined size: 10 MB',
          style: AppText.plex(13, w: FontWeight.w500, c: AppColors.label),
        ),

        const SizedBox(height: 12),

        // Upload area
        Selector<ProofOfPaymentViewModel, bool>(
          selector: (_, vm) => vm.picking,
          builder: (context, picking, _) => InkWell(
            onTap: picking ? null : () => _pick(context),
            borderRadius: BorderRadius.circular(8),
            child: CustomPaint(
              painter: _DashedBorder(),
              child: Container(
                width: double.infinity,
                height: 151,
                alignment: Alignment.center,
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(Icons.cloud_upload, size: 65, color: Color(0xFF63A4FF)),
                    const SizedBox(height: 4),
                    Text('Tap To Upload',
                        style: AppText.inter(20, w: FontWeight.w600, c: Colors.black)),
                    const SizedBox(height: 4),
                    Text(
                      picking ? 'Opening files...' : 'Choose one or multiple files',
                      style: AppText.inter(14, c: AppColors.label),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),

        // Selected files appear directly underneath the upload area.
        _SelectedFiles(onAddMore: () => _pick(context)),

        const SizedBox(height: 30),

        WideButton('Upload Document', onPressed: () async {
          final error = await vm.submit();
          if (error != null && context.mounted) snack(context, error);
        }),

        const SizedBox(height: 8),

        WideButton('Cancel', outlined: true, onPressed: () => Navigator.pop(context)),
      ],
    );
  }
}

/// The quotation / customer / total box at the top of the screen.
class _QuotationSummary extends StatelessWidget {
  const _QuotationSummary();

  @override
  Widget build(BuildContext context) {
    final bold = AppText.inter(18, w: FontWeight.w700, c: Colors.black);
    final medium = AppText.inter(18, w: FontWeight.w500, c: Colors.black);
    final quotation = context.select<WorkshopViewModel, Quotation?>((w) => w.quotation);
    final customerName =
        context.select<AuthViewModel, String?>((a) => a.customer?.fullName);
    final vehicle = context.select<WorkshopViewModel, Vehicle?>(
        (w) => w.vehicleForQuotation(w.quotation));

    return InfoCard(
      color: AppColors.surface,
      padding: const EdgeInsets.all(8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(Icons.description_outlined, size: 46),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Vehicle', style: medium),
                Text(vehicle?.displayName.trim() ?? '-', style: bold),
                Text('Registration Number', style: medium),
                Text(
                  (vehicle?.plate.isNotEmpty ?? false) ? vehicle!.plate : '-',
                  style: bold,
                ),
                Text('Quotation Job Card', style: medium),
                Text(quotation?.jobId.isNotEmpty == true ? quotation!.jobId : (quotation?.id ?? ''),
                    style: bold),
                Text('Customer', style: medium),
                Text(customerName ?? '-', style: bold),
                Text('Total Amount', style: medium),
                Text(
                  quotation == null ? '' : 'R${quotation.total.toStringAsFixed(0)}',
                  style: bold,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// The list of chosen documents with the size meter. Draws nothing until at
/// least one file has been chosen.
class _SelectedFiles extends StatelessWidget {
  final VoidCallback onAddMore;
  const _SelectedFiles({required this.onAddMore});

  @override
  Widget build(BuildContext context) {
    return Consumer<ProofOfPaymentViewModel>(
      builder: (context, vm, _) {
        final files = vm.files;
        if (files.isEmpty) return const SizedBox.shrink();

        final warn = vm.isFull;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 14),
            Row(
              children: [
                Expanded(
                  child: Text('Selected Documents (${files.length})',
                      style: AppText.inter(16, w: FontWeight.w700, c: Colors.black)),
                ),
                Text(
                  '${fmtSize(vm.totalBytes)} / 10 MB',
                  style: AppText.inter(13,
                      w: FontWeight.w600, c: warn ? Colors.red : AppColors.label),
                ),
              ],
            ),
            const SizedBox(height: 8),
            ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: LinearProgressIndicator(
                value: vm.totalBytes / ProofOfPaymentViewModel.maxTotalBytes,
                minHeight: 6,
                backgroundColor: AppColors.border,
                valueColor: AlwaysStoppedAnimation<Color>(
                    warn ? Colors.red : const Color(0xFF63A4FF)),
              ),
            ),
            const SizedBox(height: 4),
            Text('${fmtSize(vm.remainingBytes)} remaining',
                style: AppText.plex(12, c: AppColors.label)),
            const SizedBox(height: 8),
            for (var i = 0; i < files.length; i++)
              _FileTile(
                key: ValueKey('${files[i].name}_${files[i].size}_$i'),
                file: files[i],
                onRemove: () => vm.removeFile(i),
              ),
            TextButton.icon(
              onPressed: vm.picking ? null : onAddMore,
              icon: const Icon(Icons.add),
              label: const Text('Add More Files'),
            ),
          ],
        );
      },
    );
  }
}

/// One chosen document with a remove button.
class _FileTile extends StatelessWidget {
  final PlatformFile file;
  final VoidCallback onRemove;
  const _FileTile({super.key, required this.file, required this.onRemove});

  @override
  Widget build(BuildContext context) {
    final isPdf = file.extension?.toLowerCase() == 'pdf';
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: AppColors.border),
      ),
      child: Row(
        children: [
          Icon(isPdf ? Icons.picture_as_pdf_outlined : Icons.image_outlined,
              size: 32, color: const Color(0xFF63A4FF)),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(file.name,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: AppText.inter(14, w: FontWeight.w600, c: Colors.black)),
                const SizedBox(height: 4),
                Text(fmtSize(file.size), style: AppText.plex(12, c: AppColors.label)),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Remove ${file.name}',
            onPressed: onRemove,
            icon: const Icon(Icons.close, color: Colors.red),
          ),
        ],
      ),
    );
  }
}

/// Dashed rounded border around the upload area.
class _DashedBorder extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = Colors.black
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1;

    final path = Path()
      ..addRRect(
        RRect.fromRectAndRadius(Offset.zero & size, const Radius.circular(8)),
      );

    for (final metric in path.computeMetrics()) {
      double distance = 0;
      while (distance < metric.length) {
        canvas.drawPath(metric.extractPath(distance, distance + 4), paint);
        distance += 7;
      }
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
