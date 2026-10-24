/// Maps to the VEHICLE entity.
/// Represents a vehicle registered by the customer.
/// Used when booking services, viewing job status, and on the dashboard.
class Vehicle {
  final String id;
  final String make;
  final String model;
  final String plate;
  final int year;
  final int mileage;

  const Vehicle({
    required this.id,
    required this.make,
    required this.model,
    required this.plate,
    required this.year,
    this.mileage = 0,
  });
  /// Short display string used in lists and cards, e.g. "Toyota Corolla"
  String get displayName => '$make $model';

  factory Vehicle.fromMap(Map<String, dynamic> m) => Vehicle(
        id: m['id'] as String,
        make: m['make'] as String,
        model: m['model'] as String,
        plate: m['plate'] as String,
        year: m['year'] as int,
        mileage: (m['mileage'] as int?) ?? 0,
      );
  /// Serialises to a map suitable for Supabase insert/update.
  Map<String, dynamic> toMap() => {
        'id': id,
        'make': make,
        'model': model,
        'plate': plate,
        'year': year,
        'mileage': mileage,
      };
}
