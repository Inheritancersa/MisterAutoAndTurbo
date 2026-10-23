import 'dart:collection';
import 'package:flutter/foundation.dart';
import '../models/booking.dart';
import '../models/notification_item.dart';
import '../models/quotation.dart';
import '../models/repair_history.dart';
import '../models/repair_job.dart';
import '../models/vehicle.dart';
import '../repositories/customer_repository.dart';
import '../services/supabase_service.dart';
import 'auth_viewmodel.dart';
import 'package:supabase_flutter/supabase_flutter.dart';

/// ViewModel for everything the logged-in customer owns.
/// Data is loaded from / written to the same Supabase Postgres database
/// that the ASP.NET MVC staff application uses.
class WorkshopViewModel extends ChangeNotifier {
  WorkshopViewModel(this._auth, {CustomerRepository? repository})
      : _repo = repository ?? CustomerRepository() {
    _auth.addListener(_onAuthChanged);
    if (_auth.isLoggedIn) {
      refresh();
    }
  }

  final AuthViewModel _auth;
  final CustomerRepository _repo;

  final List<Vehicle> _vehicles = [];
  final List<RepairJob> _jobs = [];
  final List<RepairHistory> _history = [];
  final List<NotificationItem> _notifications = [];
  final List<Booking> _bookings = [];
  Quotation? _quotation;

  bool _loading = false;
  String? _error;

  bool get isLoading => _loading;
  String? get error => _error;

  final List<RealtimeChannel> _channels = [];

  @override
  void dispose() {
    _auth.removeListener(_onAuthChanged);
    _unsubscribeRealtime();
    super.dispose();
  }

  void _unsubscribeRealtime() {
    for (final ch in _channels) {
      try {
        SupabaseService.client.removeChannel(ch);
      } catch (_) {}
    }
    _channels.clear();
  }

  /// Listen for staff-side changes so the app updates without a manual refresh.
  void _subscribeRealtime(String customerId) {
    _unsubscribeRealtime();
    try {
      final client = SupabaseService.client;
      void onChange(PostgresChangePayload _) {
        // Debounce lightly by scheduling refresh
        refresh();
      }

      final jobCh = client
          .channel('public:JobCards:$customerId')
          .onPostgresChanges(
            event: PostgresChangeEvent.all,
            schema: 'public',
            table: 'JobCards',
            callback: onChange,
          )
          .subscribe();
      final notifCh = client
          .channel('public:Notifications:$customerId')
          .onPostgresChanges(
            event: PostgresChangeEvent.all,
            schema: 'public',
            table: 'Notifications',
            callback: onChange,
          )
          .subscribe();
      final complaintCh = client
          .channel('public:Complaints:$customerId')
          .onPostgresChanges(
            event: PostgresChangeEvent.all,
            schema: 'public',
            table: 'Complaints',
            callback: onChange,
          )
          .subscribe();
      final vehicleCh = client
          .channel('public:Vehicles:$customerId')
          .onPostgresChanges(
            event: PostgresChangeEvent.all,
            schema: 'public',
            table: 'Vehicles',
            callback: onChange,
          )
          .subscribe();
      _channels.addAll([jobCh, notifCh, complaintCh, vehicleCh]);
    } catch (e) {
      debugPrint('Realtime subscribe failed: $e');
    }
  }

  /// Job for a given vehicle id, if any.
  RepairJob? jobForVehicle(String vehicleId) {
    for (final j in _jobs) {
      if (j.vehicle.id == vehicleId) return j;
    }
    return null;
  }

  void _onAuthChanged() {
    if (_auth.isLoggedIn) {
      refresh();
    } else {
      _clear();
      notifyListeners();
    }
  }

  void _clear() {
    _unsubscribeRealtime();
    _vehicles.clear();
    _jobs.clear();
    _history.clear();
    _notifications.clear();
    _bookings.clear();
    _quotation = null;
    _error = null;
  }

  List<Vehicle> get vehicles => UnmodifiableListView(_vehicles);
  List<RepairJob> get jobs => UnmodifiableListView(_jobs);
  List<RepairHistory> get history => UnmodifiableListView(_history);
  List<NotificationItem> get notifications =>
      UnmodifiableListView(_notifications);
  List<Booking> get bookings => UnmodifiableListView(_bookings);
  Quotation? get quotation => _quotation;

  List<Vehicle> get vehiclesWithoutJob => [
        for (final v in _vehicles)
          if (!_jobs.any((j) => j.vehicle.id == v.id)) v,
      ];

  /// The vehicle a quotation is for (looked up through its job card).
  Vehicle? vehicleForQuotation(Quotation? q) {
    if (q == null) return null;
    final job = jobById(q.jobId);
    if (job == null || job.vehicle.id.isEmpty) return null;
    return job.vehicle;
  }

  RepairJob? jobById(String id) {
    for (final job in _jobs) {
      if (job.id == id) return job;
    }
    return null;
  }

  /// Fetches data from Supabase. Each fetch is isolated in its own try-catch
  /// block so that a failure in one table (e.g., missing quotations) does not
  /// prevent the vehicles from loading and displaying on the dashboard.
  Future<void> refresh() async {
    final customer = _auth.customer;
    if (customer == null) {
      _clear();
      notifyListeners();
      return;
    }
    // Avoid re-entrant refresh storms from realtime events.
    if (_loading) return;

    _loading = true;
    _error = null;
    notifyListeners();

    final cid = customer.id;
    final vehs = <Vehicle>[];

    // 1. Fetch vehicles (Critical - handles the dashboard cards)
    try {
      final fetchedVehs = await _repo.fetchVehicles(cid);
      vehs.addAll(fetchedVehs);
      _vehicles
        ..clear()
        ..addAll(vehs);
    } catch (e) {
      debugPrint('Error fetching vehicles: $e');
      _error = 'Failed to load vehicles. Check your connection.';
    }

    // 2. Fetch jobs (Used for status cards)
    try {
      final jobList = await _repo.fetchJobs(cid, vehs);
      _jobs
        ..clear()
        ..addAll(jobList);
    } catch (e) {
      debugPrint('Error fetching jobs: $e');
    }

    // 3. Fetch repair history
    try {
      final hist = await _repo.fetchHistory(cid, vehs);
      _history
        ..clear()
        ..addAll(hist);
    } catch (e) {
      debugPrint('Error fetching history: $e');
    }

    // 4. Fetch notifications (incl. reception replies on complaints)
    try {
      final notifs = await _repo.fetchNotifications(cid);
      final complaintNotifs = await _repo.fetchComplaintNotifications(cid);
      _notifications
        ..clear()
        ..addAll(notifs)
        ..addAll(complaintNotifs)
        // Newest first (undated items last).
        ..sort((a, b) {
          final x = a.at, y = b.at;
          if (x == null && y == null) return 0;
          if (x == null) return 1;
          if (y == null) return -1;
          return y.compareTo(x);
        });
    } catch (e) {
      debugPrint('Error fetching notifications: $e');
    }

    // 5. Fetch bookings
    try {
      final books = await _repo.fetchBookings(cid);
      _bookings
        ..clear()
        ..addAll(books);
    } catch (e) {
      debugPrint('Error fetching bookings: $e');
    }

    // 6. Fetch quotations
    try {
      final quote = await _repo.fetchLatestQuotation(cid);
      _quotation = quote;
    } catch (e) {
      debugPrint('Error fetching quotations: $e');
    }

    _loading = false;
    notifyListeners();

    // Keep data live when reception updates jobs / complaints / notifications.
    _subscribeRealtime(cid);
  }

  Future<String?> addVehicle(Vehicle v) async {
    final customer = _auth.customer;
    if (customer == null) return 'Not logged in';

    try {
      final created = await _repo.addVehicle(
        customerId: customer.id,
        make: v.make,
        model: v.model,
        plate: v.plate,
        year: v.year,
        mileage: v.mileage,
      );
      _vehicles.add(created);
      notifyListeners();
      return null;
    } catch (e) {
      return 'Could not save vehicle: $e';
    }
  }

  Future<String?> addBooking(Booking b) async {
    final customer = _auth.customer;
    if (customer == null) return 'Not logged in';

    try {
      final parts = b.time.split(':');
      final scheduled = DateTime(
        b.date.year,
        b.date.month,
        b.date.day,
        int.tryParse(parts.first) ?? 9,
        parts.length > 1 ? (int.tryParse(parts[1]) ?? 0) : 0,
      );

      await _repo.createAppointment(
        customerId: customer.id,
        vehicleId: b.vehicle.id,
        scheduledAt: scheduled,
        serviceLabel: b.service.label,
        notes: b.notes,
      );

      _bookings.insert(0, b);
      notifyListeners();
      return null;
    } catch (e) {
      return 'Could not book service: $e';
    }
  }

  void addNotification(NotificationItem n) {
    _notifications.insert(0, n);
    notifyListeners();
  }

  Future<String?> decideQuotation(bool accept) async {
    final q = _quotation;
    if (q == null) return 'No quotation to respond to';

    try {
      await _repo.respondToQuotation(quotationId: q.id, accept: accept);
      _quotation = q.copyWith(
          status: accept ? QuotationStatus.accepted : QuotationStatus.rejected);
      _notifications.insert(
          0,
          accept
              ? NotificationItem('Quotation Approved', 'You Approved ${q.id}', at: DateTime.now())
              : NotificationItem('Quotation Rejected', 'You Rejected ${q.id}', at: DateTime.now()));
      notifyListeners();
      return null;
    } catch (e) {
      return 'Could not update quotation: $e';
    }
  }

  Future<String?> submitComplaint({
    required String subject,
    required String description,
    String jobCardId = '',
    String vehicleId = '',
  }) async {
    final customer = _auth.customer;
    if (customer == null) return 'Not logged in';

    try {
      await _repo.submitComplaint(
        customerId: customer.id,
        subject: subject,
        description: description,
        jobCardId: jobCardId,
        vehicleId: vehicleId,
      );
      _notifications.insert(
        0,
        NotificationItem(
          'Complaint Submitted',
          'Your complaint was sent to the workshop.',
          at: DateTime.now(),
        ),
      );
      notifyListeners();
      return null;
    } catch (e) {
      return 'Could not submit complaint: $e';
    }
  }

  Future<String?> submitProofOfPayment({
    required String fileName,
    required List<int> bytes,
    double amount = 0,
  }) async {
    final customer = _auth.customer;
    if (customer == null) return 'Not logged in';

    try {
      final url = await _repo.uploadProofFile(
        customerId: customer.id,
        fileName: fileName,
        bytes: bytes,
      );
      final quoteId = _quotation?.id ?? '';
      await _repo.submitProofOfPayment(
        customerId: customer.id,
        quotationId: quoteId,
        amount: amount > 0 ? amount : (_quotation?.total ?? 0),
        storagePath: url,
      );
      _notifications.insert(
        0,
        NotificationItem(
          'Proof Of Payment',
          'Your proof of payment was uploaded.',
          at: DateTime.now(),
        ),
      );
      notifyListeners();
      return null;
    } catch (e) {
      return 'Upload failed: $e';
    }
  }
}
