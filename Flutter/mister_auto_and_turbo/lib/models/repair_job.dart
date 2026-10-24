import 'vehicle.dart';

/// Job lifecycle stages shown on the "Current Status" screen.
/// Matches the staff app status list:
/// Inspection → Quotation → In Progress → Quality Check → Ready for Collection → Collected
enum JobStage {
  inspection,
  quotation,
  inProgress,
  qualityCheck,
  readyForCollection,
  collected,
}

extension JobStageLabel on JobStage {
  String get label => switch (this) {
        JobStage.inspection => 'Inspection',
        JobStage.quotation => 'Quotation',
        JobStage.inProgress => 'In Progress',
        JobStage.qualityCheck => 'Quality Check',
        JobStage.readyForCollection => 'Ready for Collection',
        JobStage.collected => 'Collected',
      };
}

/// Maps to JOB_CARD (same Supabase table the ASP.NET staff app writes to).
class RepairJob {
  final String id;
  final Vehicle vehicle;
  final String title;
  final JobStage stage;
  final bool collected;
  /// Staff diagnosis from JobCards.Diagnosis (RecordDiagnosis / Edit).
  final String diagnosis;
  /// Staff repair notes from JobCards.RepairNotes (AddRepairNotes / Edit).
  final String repairNotes;

  const RepairJob({
    required this.id,
    required this.vehicle,
    required this.title,
    required this.stage,
    this.collected = false,
    this.diagnosis = '',
    this.repairNotes = '',
  });

  bool get isComplete =>
      collected || stage == JobStage.collected || stage == JobStage.readyForCollection;

  bool get hasDiagnosis => diagnosis.trim().isNotEmpty;
  bool get hasRepairNotes => repairNotes.trim().isNotEmpty;

  /// Label used on the dashboard vehicle card.
  String get summaryLabel {
    if (collected || stage == JobStage.collected) return 'Collected';
    return stage.label;
  }

  /// A stage is done when the job has reached or passed that stage.
  bool stageDone(JobStage s) {
    if (collected || stage == JobStage.collected) return true;
    return s.index <= stage.index;
  }
}
