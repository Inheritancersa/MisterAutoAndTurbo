import 'package:flutter/foundation.dart';
import '../models/repair_job.dart';
import '../models/vehicle.dart';
import 'workshop_viewmodel.dart';

/// State of the Complaints screen: the chosen type and whether the complaint
/// has been submitted.
class ComplaintViewModel extends ChangeNotifier {
  ComplaintViewModel(this._workshop);

  final WorkshopViewModel _workshop;

  static const List<String> types = [
    'Service Quality',
    'Delay In Repair',
    'Pricing / Quotation',
    'Staff Conduct',
    'Damage To Vehicle',
    'Parts Issue',
    'Other',
  ];

  String? _type;
  String? _vehicleId;
  String? _jobId;
  bool _sent = false;
  bool _busy = false;

  String? get type => _type;
  bool get sent => _sent;
  bool get isBusy => _busy;

  String? get vehicleId => _vehicleId;
  String? get jobId => _jobId;

  List<Vehicle> get vehicles => _workshop.vehicles;

  /// Jobs shown in the picker: only the chosen vehicle's jobs, if one is set.
  List<RepairJob> get jobs => [
        for (final j in _workshop.jobs)
          if (_vehicleId == null || j.vehicle.id == _vehicleId) j,
      ];

  void selectVehicle(String? id) {
    _vehicleId = id;
    // Drop the job if it belongs to a different vehicle.
    if (_jobId != null && !jobs.any((j) => j.id == _jobId)) _jobId = null;
    notifyListeners();
  }

  void selectJob(String? id) {
    _jobId = id;
    // Picking a job also fills in its vehicle.
    if (id != null) {
      for (final j in _workshop.jobs) {
        if (j.id == id) _vehicleId = j.vehicle.id;
      }
    }
    notifyListeners();
  }

  void selectType(String? value) {
    _type = value;
    notifyListeners();
  }

  /// Saves the complaint to Supabase and flips to the "submitted" view.
  Future<String?> submit({String description = ''}) async {
    if (_type == null) return 'Please select a complaint type';
    _busy = true;
    notifyListeners();
    final err = await _workshop.submitComplaint(
      subject: _type!,
      description: description.isEmpty ? _type! : description,
      jobCardId: _jobId ?? '',
      vehicleId: _vehicleId ?? '',
    );
    _busy = false;
    if (err == null) {
      _sent = true;
    }
    notifyListeners();
    return err;
  }
}