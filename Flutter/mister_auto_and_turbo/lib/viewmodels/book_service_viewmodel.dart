import 'package:flutter/foundation.dart';
import '../models/booking.dart';
import '../models/vehicle.dart';
import 'workshop_viewmodel.dart';

/// The three steps of the "Book A Service" flow.
enum BookingStep { chooseService, pickTime, confirmed }

/// State of the Book A Service screen:
/// STEP 1 select a vehicle and service type,
/// STEP 2 pick a date and time,
/// STEP 3 show the booking confirmation.
class BookServiceViewModel extends ChangeNotifier {
  BookServiceViewModel(this._workshop);

  final WorkshopViewModel _workshop;

  /// Available workshop appointment times.
  static const List<String> slots = [
    '09:00', '10:00', '11:00', '12:00',
    '13:00', '14:00', '15:00', '16:00',
  ];

  BookingStep _step = BookingStep.chooseService;
  Vehicle? _vehicle;
  ServiceType? _service;
  DateTime? _date;
  String? _time;
  Booking? _confirmed;

  BookingStep get step => _step;
  Vehicle? get vehicle => _vehicle;
  ServiceType? get service => _service;
  DateTime? get date => _date;
  String? get time => _time;

  /// Non-null after a booking has been confirmed.
  Booking? get confirmed => _confirmed;

  void selectVehicle(Vehicle v) {
    _vehicle = v;
    notifyListeners();
  }

  void selectService(ServiceType s) {
    _service = s;
    notifyListeners();
  }

  void selectDate(DateTime d) {
    _date = d;
    notifyListeners();
  }

  void selectTime(String t) {
    _time = t;
    notifyListeners();
  }

  /// Step 1 -> step 2. Returns an error message, or null on success.
  String? next() {
    if (_vehicle == null || _service == null) {
      return 'Select a vehicle and a service';
    }
    _step = BookingStep.pickTime;
    notifyListeners();
    return null;
  }

  /// Step 2 -> step 1.
  void back() {
    _step = BookingStep.chooseService;
    notifyListeners();
  }

  /// Validates the choices, saves the booking and moves to step 3.
  /// Returns an error message, or null on success.
  Future<String?> confirm(String notes) async {
    if (_vehicle == null || _service == null) {
      return 'Select a vehicle and a service';
    }
    if (_date == null || _time == null) {
      return 'Pick a date and time';
    }
    final booking = Booking(
      vehicle: _vehicle!,
      service: _service!,
      date: _date!,
      time: _time!,
      notes: notes.trim(),
    );
    final err = await _workshop.addBooking(booking);
    if (err != null) return err;
    _confirmed = booking;
    _step = BookingStep.confirmed;
    notifyListeners();
    return null;
  }
}
