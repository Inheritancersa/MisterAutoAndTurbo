enum QuotationStatus { pending, accepted, rejected }

class QuotationLine {
  final String item;
  final double amount;
  const QuotationLine(this.item, this.amount);
}

/// Maps to QUOTATION. Supports the customer-supplied part saving (FR-10).
class Quotation {
  final String id;
  final String jobId;
  final List<QuotationLine> lines;
  final double customerSuppliedSaving;
  final QuotationStatus status;

  const Quotation({
    required this.id,
    required this.jobId,
    required this.lines,
    this.customerSuppliedSaving = 0,
    this.status = QuotationStatus.pending,
  });

  double get total => lines.fold(0, (s, l) => s + l.amount);
  double get totalIfCustomerSupplies => total - customerSuppliedSaving;

  Quotation copyWith({QuotationStatus? status}) => Quotation(
        id: id,
        jobId: jobId,
        lines: lines,
        customerSuppliedSaving: customerSuppliedSaving,
        status: status ?? this.status,
      );
}

String rand(double v) => 'R${v.toStringAsFixed(0)}.';

