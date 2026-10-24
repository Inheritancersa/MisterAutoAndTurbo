/// Customer profile model.
///
/// Maps to the CUSTOMER / profiles table in Supabase.
/// Used throughout the app for display name, contact details, and ownership
/// of vehicles, bookings, and repair jobs.
class Customer {
  final String id;
  final String fullName;
  final String email;
  final String phone;
  final String address;

  const Customer({
    required this.id,
    required this.fullName,
    required this.email,
    required this.phone,
    this.address = '',
  });

  String get firstName => fullName.trim().split(' ').first;
   /// Returns a copy of this customer with selected fields replaced.
  /// Useful for optimistic UI updates after profile edits.
  Customer copyWith({
    String? fullName,
    String? email,
    String? phone,
    String? address,
  }) =>
      Customer(
        id: id,
        fullName: fullName ?? this.fullName,
        email: email ?? this.email,
        phone: phone ?? this.phone,
        address: address ?? this.address,
      );

  factory Customer.fromMap(Map<String, dynamic> m) => Customer(
        id: m['id'] as String,
        fullName: m['full_name'] as String,
        email: m['email'] as String,
        phone: m['phone'] as String,
        address: (m['address'] as String?) ?? '',
      );
    // Serialises to a map suitable for Supabase insert/update.
  Map<String, dynamic> toMap() => {
        'id': id,
        'full_name': fullName,
        'email': email,
        'phone': phone,
        'address': address,
      };
}
