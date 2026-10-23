import 'vehicle.dart';

/// Maps to APPOINTMENT. Service types come from the Customer use case diagram.
enum ServiceType { diagnostic, oilAndFilter, repair, maintenance }

extension ServiceTypeLabel on ServiceType {
  String get label => switch (this) {
        ServiceType.diagnostic => 'Diagnostic',
        ServiceType.oilAndFilter => 'Oil & Filter Exchange',
        ServiceType.repair => 'Repair',
        ServiceType.maintenance => 'Maintenance',
      };
}

class Booking {
  final Vehicle vehicle;
  final ServiceType service;
  final DateTime date;
  final String time;
  final String notes;
  const Booking({
    required this.vehicle,
    required this.service,
    required this.date,
    required this.time,
    this.notes = '',
  });
}
