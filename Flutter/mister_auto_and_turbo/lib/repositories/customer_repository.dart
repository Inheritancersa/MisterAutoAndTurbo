import 'dart:typed_data';

import 'package:flutter/foundation.dart';
import 'package:supabase_flutter/supabase_flutter.dart';
import '../models/customer.dart';
import '../models/notification_item.dart';
import '../models/quotation.dart';
import '../models/repair_history.dart';
import '../models/repair_job.dart';
import '../models/vehicle.dart';
import '../models/booking.dart';
import '../services/supabase_service.dart';
import '../utils/helpers.dart';

/// Data access layer for the customer Flutter app.
///
/// Talks to the same Postgres tables that the ASP.NET MVC staff app uses
/// (Customers, Vehicles, Appointments, JobCards, Quotations, Payments,
/// Complaints, Notifications, …).
///
/// Table / column names match the EF Core models (PascalCase). If a query
/// fails with "column does not exist", run the SQL in `supabase/rls_and_views.sql`
/// which also creates lower-case views for convenience.
class CustomerRepository {
  SupabaseClient get _db => SupabaseService.client;

  // ---------------------------------------------------------------------------
  // Auth helpers – link Supabase Auth user → Customers row
  // ---------------------------------------------------------------------------

  /// Finds the customer profile for the currently signed-in auth user.
  ///
  /// Strategy (in order):
  ///   1. Match Customers.UserAccountId == auth.uid()
  ///   2. Match Customers.Email == auth user's email (case-insensitive)
  ///
  /// If matched by (2) and UserAccountId is still null, we link it so that
  /// subsequent RLS checks using auth.uid() succeed.
  Future<Customer?> fetchCurrentCustomer() async {
    final user = _db.auth.currentUser;
    if (user == null) return null;

    // 1. Match by UserAccountId
    final byAuth = await _db
        .from('Customers')
        .select()
        .eq('UserAccountId', user.id)
        .maybeSingle();

    if (byAuth != null) return _customerFromRow(byAuth);

    // 2. Fall back to email match
    final email = user.email ?? '';
    if (email.isEmpty) return null;

    final byEmail = await _db
        .from('Customers')
        .select()
        .ilike('Email', email)
        .maybeSingle();

    if (byEmail == null) return null;

    // Link UserAccountId now so future RLS checks pass.
    if ((byEmail['UserAccountId'] as String?)?.isEmpty ?? true) {
      try {
        await _db.from('Customers').update({'UserAccountId': user.id}).eq(
            'Id', byEmail['Id'] as String);
        debugPrint('Linked Customer ${byEmail['Id']} → auth ${user.id}');
      } on PostgrestException catch (e) {
        debugPrint(
            'LINK FAILED (fetchCurrentCustomer): ${e.code} ${e.message}');
      }
    }

    return _customerFromRow(byEmail);
  }

  /// Creates a Customers row after a successful Supabase Auth sign-up.
  ///
  /// If a row already exists for this auth user or email, returns / links it
  /// instead of inserting a duplicate.
  Future<Customer> createCustomerProfile({
    required String authUserId,
    required String fullName,
    required String email,
    required String phone,
    String address = '',
  }) async {
    // 1. Already linked to this auth user?
    final existingByAuth = await _db
        .from('Customers')
        .select()
        .eq('UserAccountId', authUserId)
        .maybeSingle();
    if (existingByAuth != null) return _customerFromRow(existingByAuth);

    // 2. Existing by email? Link it.
    final existingByEmail = await _db
        .from('Customers')
        .select()
        .ilike('Email', email)
        .maybeSingle();
    if (existingByEmail != null) {
      try {
        await _db.from('Customers').update({'UserAccountId': authUserId}).eq(
            'Id', existingByEmail['Id'] as String);
        debugPrint(
            'Linked existing Customer ${existingByEmail['Id']} → auth $authUserId');
      } on PostgrestException catch (e) {
        debugPrint(
            'LINK FAILED (createCustomerProfile): ${e.code} ${e.message}');
      }
      return _customerFromRow(existingByEmail);
    }

    // 3. Create new.
    final parts = fullName.trim().split(RegExp(r'\s+'));
    final name = parts.isNotEmpty ? parts.first : fullName;
    final surname = parts.length > 1 ? parts.sublist(1).join(' ') : '';

    final id = 'cust-${DateTime.now().millisecondsSinceEpoch}';

    final row = {
      'Id': id,
      'UserAccountId': authUserId,
      'Name': name,
      'Surname': surname,
      'Phone': phone,
      'Email': email,
      'Address': address,
    };

    await _db.from('Customers').insert(row);
    return Customer(
      id: id,
      fullName: fullName.trim(),
      email: email,
      phone: phone,
    );
  }


  /// Updates the logged-in customer's profile fields in Supabase.
  Future<Customer> updateCustomerProfile({
    required String customerId,
    required String fullName,
    required String phone,
    required String address,
  }) async {
    final parts = fullName.trim().split(RegExp(r'\s+'));
    final name = parts.isNotEmpty ? parts.first : fullName;
    final surname = parts.length > 1 ? parts.sublist(1).join(' ') : '';

    await _db.from('Customers').update({
      'Name': name,
      'Surname': surname,
      'Phone': phone,
      'Address': address,
    }).eq('Id', customerId);

    final row = await _db.from('Customers').select().eq('Id', customerId).single();
    return _customerFromRow(row as Map<String, dynamic>);
  }

  /// Complaints that reception has responded to (StaffNotes / status change).
  Future<List<NotificationItem>> fetchComplaintNotifications(String customerId) async {
    final rows = await _db
        .from('Complaints')
        .select()
        .eq('CustomerId', customerId)
        .order('CreatedAt', ascending: false)
        .limit(30);

    final items = <NotificationItem>[];
    for (final r in (rows as List)) {
      final m = r as Map<String, dynamic>;
      final status = (m['Status'] as String?) ?? 'Open';
      final staffNotes = (m['StaffNotes'] as String?)?.trim() ?? '';
      final subject = (m['Subject'] as String?) ?? 'Complaint';
      // Use the reply time when the workshop has one, else the creation time.
      final at = parseDbTime(m['UpdatedAt'] ?? m['RespondedAt'] ?? m['CreatedAt']) ??
          parseDbTime(m['CreatedAt']);

      // Notify when reception has replied or closed/updated the complaint.
      if (staffNotes.isNotEmpty || status.toLowerCase() != 'open') {
        final body = staffNotes.isNotEmpty
            ? 'Reception: $staffNotes'
            : 'Your complaint "$subject" is now: $status';
        items.add(NotificationItem(
          'Complaint update: $subject',
          body,
          at: at,
          highlighted: status.toLowerCase() == 'open' || status.toLowerCase() == 'in progress',
        ));
      }
    }
    return items;
  }

  // ---------------------------------------------------------------------------
  // Vehicles
  // ---------------------------------------------------------------------------

  Future<List<Vehicle>> fetchVehicles(String customerId) async {
    final rows = await _db
        .from('Vehicles')
        .select()
        .eq('CustomerId', customerId)
        .order('Make');

    debugPrint(
        'fetchVehicles(customerId=$customerId) → ${(rows as List).length} rows');

    return rows.map((r) => _vehicleFromRow(r as Map<String, dynamic>)).toList();
  }

  Future<Vehicle> addVehicle({
    required String customerId,
    required String make,
    required String model,
    required String plate,
    required int year,
    int mileage = 0,
  }) async {
    final id = 'veh-${DateTime.now().millisecondsSinceEpoch}';
    final row = {
      'Id': id,
      'CustomerId': customerId,
      'RegistrationNumber': plate,
      'Make': make,
      'Model': model,
      'Year': year,
      'Mileage': mileage,
    };
    await _db.from('Vehicles').insert(row);
    return Vehicle(id: id, make: make, model: model, plate: plate, year: year, mileage: mileage);
  }

  // ---------------------------------------------------------------------------
  // Appointments / Bookings
  // ---------------------------------------------------------------------------

  Future<List<Booking>> fetchBookings(String customerId) async {
    final rows = await _db
        .from('Appointments')
        .select('*, Vehicles(*)')
        .eq('CustomerId', customerId)
        .order('ScheduledAt', ascending: false);

    final list = <Booking>[];
    for (final r in rows as List) {
      final map = r as Map<String, dynamic>;
      final vehMap = map['Vehicles'] as Map<String, dynamic>?;
      final vehicle = vehMap != null
          ? _vehicleFromRow(vehMap)
          : Vehicle(
              id: map['VehicleId'] as String? ?? '',
              make: 'Unknown',
              model: '',
              plate: '',
              year: 0,
            );

      final scheduled =
          DateTime.tryParse(map['ScheduledAt']?.toString() ?? '') ??
              DateTime.now();
      final time =
          '${scheduled.hour.toString().padLeft(2, '0')}:${scheduled.minute.toString().padLeft(2, '0')}';

      list.add(Booking(
        vehicle: vehicle,
        service:
            ServiceType.maintenance, // detailed type not stored on Appointment
        date: scheduled,
        time: time,
        notes: map['Status']?.toString() ?? '',
      ));
    }
    return list;
  }

  Future<void> createAppointment({
    required String customerId,
    required String vehicleId,
    required DateTime scheduledAt,
    required String serviceLabel,
    String notes = '',
  }) async {
    final id = 'apt-${DateTime.now().millisecondsSinceEpoch}';
    await _db.from('Appointments').insert({
      'Id': id,
      'CustomerId': customerId,
      'VehicleId': vehicleId,
      'JobCardId': '',
      'CreatedById': customerId,
      'ScheduledAt': scheduledAt.toUtc().toIso8601String(),
      'Status': 'Scheduled',
      // Shown on ASP.NET Appointments list/details
      'ServiceType': serviceLabel,
      'Description': notes,
    });

    // Also record a ServiceRequest so staff can see the requested work.
    final srId = 'sr-${DateTime.now().millisecondsSinceEpoch}';
    await _db.from('ServiceRequests').insert({
      'Id': srId,
      'CustomerId': customerId,
      'VehicleId': vehicleId,
      'RequestedService': serviceLabel,
      'Description': notes,
      'Status': 'Open',
      'CreatedAt': DateTime.now().toUtc().toIso8601String(),
    });
  }

  // ---------------------------------------------------------------------------
  // Job cards (status)
  // ---------------------------------------------------------------------------

  Future<List<RepairJob>> fetchJobs(
      String customerId, List<Vehicle> vehicles) async {
    final rows = await _db
        .from('JobCards')
        .select()
        .eq('CustomerId', customerId)
        .order('CreatedAt', ascending: false);

    final byId = {for (final v in vehicles) v.id: v};

    return (rows as List).map((r) {
      final map = r as Map<String, dynamic>;
      // Note: MVC model has typo "VehichleId"
      final vehicleId = (map['VehichleId'] ?? map['VehicleId'] ?? '') as String;
      final vehicle = byId[vehicleId] ??
          Vehicle(
            id: vehicleId,
            make: 'Unknown',
            model: '',
            plate: '',
            year: 0,
          );

      final status = (map['Status'] as String?) ?? 'Inspection';
      final stage = _mapJobStage(status);
      final collected = status == 'Collected';

      final diagnosis = (map['Diagnosis'] as String?)?.trim() ?? '';
      final repairNotes = (map['RepairNotes'] as String?)?.trim() ?? '';

      return RepairJob(
        id: map['Id'] as String,
        vehicle: vehicle,
        title: diagnosis.isNotEmpty ? diagnosis : 'Service',
        stage: stage,
        collected: collected,
        diagnosis: diagnosis,
        repairNotes: repairNotes,
      );
    }).toList();
  }

  JobStage _mapJobStage(String status) {
    // Align with staff status dropdown (vehicle_status_viewmodel photo).
    switch (status.trim()) {
      case 'Inspection':
        return JobStage.inspection;
      case 'Quotation':
      case 'Quotation Approved':
        return JobStage.quotation;
      case 'In Progress':
        return JobStage.inProgress;
      case 'Quality Check':
        return JobStage.qualityCheck;
      case 'Ready for Collection':
      case 'Ready For Collection':
        return JobStage.readyForCollection;
      case 'Collected':
        return JobStage.collected;
      default:
        return JobStage.inspection;
    }
  }

  // ---------------------------------------------------------------------------
  // Quotations
  // ---------------------------------------------------------------------------

  Future<Quotation?> fetchLatestQuotation(String customerId) async {
    final rows = await _db
        .from('Quotations')
        .select()
        .eq('CustomerId', customerId)
        // Customers only see quotations staff have sent (or that they already answered).
        .inFilter('Status', ['Sent', 'Approved', 'Rejected'])
        .order('CreatedAt', ascending: false)
        .limit(1);

    if ((rows as List).isEmpty) return null;
    return _quotationFromRow(rows.first /*as Map<String, dynamic>*/);
  }

  Future<void> respondToQuotation({
    required String quotationId,
    required bool accept,
  }) async {
    // Only a quotation that is still 'Sent' can be answered, so a late tap
    // can never overwrite a decision already saved (and shown in the staff app).
    await _db
        .from('Quotations')
        .update({
          'Status': accept ? 'Approved' : 'Rejected',
          'RespondedAt': DateTime.now().toUtc().toIso8601String(),
        })
        .eq('Id', quotationId)
        .eq('Status', 'Sent');
  }

  Quotation _quotationFromRow(Map<String, dynamic> m) {
    final labour = _toDouble(m['LabourCost']);
    final parts = _toDouble(m['PartsCost']);
    final saving = _toDouble(m['CustomerSuppliedDifference']);
    final total = _toDouble(m['TotalCost']);
    final statusStr = (m['Status'] as String?) ?? 'Pending';

    QuotationStatus status;
    switch (statusStr) {
      case 'Approved':
        status = QuotationStatus.accepted;
        break;
      case 'Rejected':
        status = QuotationStatus.rejected;
        break;
      default:
        status = QuotationStatus.pending;
    }

    final lines = <QuotationLine>[
      if (labour > 0) QuotationLine('Labour', labour),
      if (parts > 0) QuotationLine('Parts', parts),
    ];
    if (lines.isEmpty && total > 0) {
      lines.add(
          QuotationLine((m['Description'] as String?) ?? 'Service', total));
    }

    return Quotation(
      id: m['Id'] as String,
      jobId: (m['JobCardId'] as String?) ?? '',
      lines: lines,
      customerSuppliedSaving: saving,
      status: status,
    );
  }

  // ---------------------------------------------------------------------------
  // History (completed / collected jobs)
  // ---------------------------------------------------------------------------

  Future<List<RepairHistory>> fetchHistory(
      String customerId, List<Vehicle> vehicles) async {
    final jobs = await fetchJobs(customerId, vehicles);
        // Include completed jobs and any job that already has staff diagnosis/notes.
    return jobs
        .where((j) => j.isComplete || j.hasDiagnosis || j.hasRepairNotes)
        .map((j) => RepairHistory(
              title: j.title,
              vehicleName: j.vehicle.displayName,
              status: j.summaryLabel,
              date: null,
              cost: null,
              diagnosis: j.diagnosis,
              repairNotes: j.repairNotes,
            ))
        .toList();
  }

  // ---------------------------------------------------------------------------
  // Notifications
  // ---------------------------------------------------------------------------

  Future<List<NotificationItem>> fetchNotifications(String customerId) async {
    // MVC Notification uses UserAccountId – we store the customer id or auth id there.
    final rows = await _db
        .from('Notifications')
        .select()
        .or('UserAccountId.eq.$customerId')
        .order('CreatedAt', ascending: false)
        .limit(50);

    return (rows as List).map((r) {
      final m = r as Map<String, dynamic>;
      return NotificationItem(
        (m['Type'] as String?) ?? 'Update',
        (m['Message'] as String?) ?? '',
        at: parseDbTime(m['CreatedAt']),
        highlighted: !(m['IsRead'] as bool? ?? true),
      );
    }).toList();
  }

  // ---------------------------------------------------------------------------
  // Complaints
  // ---------------------------------------------------------------------------

  Future<void> submitComplaint({
    required String customerId,
    required String subject,
    required String description,
    String jobCardId = '',
    String vehicleId = '',
  }) async {
    final id = 'cmp-${DateTime.now().millisecondsSinceEpoch}';
    await _db.from('Complaints').insert({
      'Id': id,
      'CustomerId': customerId,
      'JobCardId': jobCardId,
      'VehicleId': vehicleId,
      'Subject': subject,
      'Description': description,
      'Status': 'Open',
      'Source': 'Mobile',
      'StaffNotes': '',
      'CreatedAt': DateTime.now().toUtc().toIso8601String(),
    });
  }

  // ---------------------------------------------------------------------------
  // Proof of payment
  // ---------------------------------------------------------------------------

  Future<void> submitProofOfPayment({
    required String customerId,
    required String quotationId,
    required double amount,
    required String storagePath,
  }) async {
    final id = 'pay-${DateTime.now().millisecondsSinceEpoch}';
    await _db.from('Payments').insert({
      'Id': id,
      'QuotationId': quotationId,
      'CustomerId': customerId,
      'RecordedById': customerId,
      'Amount': amount,
      'Method': 'EFT',
      'ProofOfPaymentUrl': storagePath,
      'Status': 'Recorded',
      'PaidAt': DateTime.now().toUtc().toIso8601String(),
    });
  }

  Future<String> uploadProofFile({
    required String customerId,
    required String fileName,
    required List<int> bytes,
  }) async {
    final path =
        'proofs/$customerId/${DateTime.now().millisecondsSinceEpoch}_$fileName';
    await _db.storage.from('proof-of-payment').uploadBinary(
          path,
          Uint8List.fromList(bytes),
          fileOptions: const FileOptions(upsert: true),
        );
    return _db.storage.from('proof-of-payment').getPublicUrl(path);
  }

  // ---------------------------------------------------------------------------
  // Mappers
  // ---------------------------------------------------------------------------

  Customer _customerFromRow(Map<String, dynamic> m) {
    final name = (m['Name'] as String?) ?? '';
    final surname = (m['Surname'] as String?) ?? '';
    final fullName = '$name $surname'.trim();
    return Customer(
      id: m['Id'] as String,
      fullName: fullName.isEmpty ? (m['Email'] as String? ?? '') : fullName,
      email: (m['Email'] as String?) ?? '',
      phone: (m['Phone'] as String?) ?? '',
      address: (m['Address'] as String?) ?? '',
    );
  }

  Vehicle _vehicleFromRow(Map<String, dynamic> m) {
    final mileageRaw = m['Mileage'] ?? m['mileage'] ?? 0;
    final mileage = mileageRaw is int
        ? mileageRaw
        : int.tryParse(mileageRaw.toString()) ?? 0;
    return Vehicle(
      id: m['Id'] as String,
      make: (m['Make'] as String?) ?? '',
      model: (m['Model'] as String?) ?? '',
      plate:
          (m['RegistrationNumber'] as String?) ?? (m['plate'] as String?) ?? '',
      year: (m['Year'] as int?) ?? 0,
      mileage: mileage,
    );
  }

  double _toDouble(dynamic v) {
    if (v == null) return 0;
    if (v is num) return v.toDouble();
    return double.tryParse(v.toString()) ?? 0;
  }
}
