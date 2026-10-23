/// Maps to a completed/ongoing REPAIR entry per vehicle (SN-05).
class RepairHistory {
  final String title;
  final String vehicleName;
  final String status;
  final DateTime? date;
  final double? cost;
  final String diagnosis;
  final String repairNotes;

  const RepairHistory({
    required this.title,
    required this.vehicleName,
    required this.status,
    this.date,
    this.cost,
    this.diagnosis = '',
    this.repairNotes = '',
  });

  String get subtitle => '$vehicleName · $status';

  bool get hasDiagnosis => diagnosis.trim().isNotEmpty;
  bool get hasRepairNotes => repairNotes.trim().isNotEmpty;
}
