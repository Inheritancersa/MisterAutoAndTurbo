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

  Map<String, dynamic> toMap() => {
        'id': id,
        'full_name': fullName,
        'email': email,
        'phone': phone,
        'address': address,
      };
}
