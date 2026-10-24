import 'vehicle.dart';

/// Maps to APPOINTMENT. Service types come from the Customer use case diagram.
/// /// Represents a service booking made by the customer for one of their vehicles.
/// The booking captures which vehicle, what type of service, the preferred date/time,
/// and any free-text notes the customer wants the workshop to see.
enum ServiceType { diagnostic, oilAndFilter, repair, maintenance }

extension ServiceTypeLabel on ServiceType {
  String get label => switch (this) {
        ServiceType.diagnostic => 'Diagnostic',
        ServiceType.oilAndFilter => 'Oil & Filter Exchange',
        ServiceType.repair => 'Repair',
        ServiceType.maintenance => 'Maintenance',
      };
}
/// Immutable data class for a single service appointment.
///
/// Fields:
/// - [vehicle] : the customer's registered vehicle being booked
/// - [service] : one of the supported service types
/// - [date]    : calendar date chosen by the customer
/// - [time]    : time slot as a display string (e.g. "09:00")
/// - [notes]   : optional free-text instructions from the customer
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
