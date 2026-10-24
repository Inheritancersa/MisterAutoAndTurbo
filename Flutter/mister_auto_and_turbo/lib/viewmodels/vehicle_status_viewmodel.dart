import 'package:flutter/foundation.dart';
import '../models/repair_job.dart';
import '../models/vehicle.dart';
import 'workshop_viewmodel.dart';

/// State of the Current Status screen for one vehicle.
///
/// A newly registered vehicle can open this screen even if no repair job has
/// been created yet, so [job] may be null.
class VehicleStatusViewModel extends ChangeNotifier {
  VehicleStatusViewModel(this._workshop,
      {required this.vehicle, RepairJob? job})
      : _job = job {
    // Re-sync whenever workshop data changes (refresh / realtime).
    _workshop.addListener(_onWorkshopChanged);
  }

  final WorkshopViewModel _workshop;
  final Vehicle vehicle;
  RepairJob? _job;
  bool _refreshing = false;

  RepairJob? get job => _job;
  bool get isRefreshing => _refreshing;

  /// Job card ID for this vehicle once reception has registered the job card;
  /// null until then.
  String? get jobCardId => _job?.id;

  /// Text under the vehicle name, e.g. "CA 123 GP · In Progress".
  String get summary =>
      '${vehicle.plate} · ${_job?.summaryLabel ?? 'No active repair'}';

  void _onWorkshopChanged() {
    final latest = _workshop.jobForVehicle(vehicle.id);
    // Prefer job that was already tied by id if still present.
    if (_job != null) {
      final byId = _workshop.jobById(_job!.id);
      if (byId != null) {
        _job = byId;
        notifyListeners();
        return;
      }
    }
    if (latest != null && latest != _job) {
      _job = latest;
      notifyListeners();
    }
  }

  /// Reloads workshop data from Supabase, then picks the latest job for this vehicle.
  Future<String> refresh() async {
    _refreshing = true;
    notifyListeners();
    try {
      await _workshop.refresh();
      final byId = _job != null ? _workshop.jobById(_job!.id) : null;
      _job = byId ?? _workshop.jobForVehicle(vehicle.id);
      if (_job == null) {
        return 'No repair job has been created for this vehicle yet.';
      }
      return 'Status refreshed';
    } catch (e) {
      return 'Could not refresh: $e';
    } finally {
      _refreshing = false;
      notifyListeners();
    }
  }

  @override
  void dispose() {
    _workshop.removeListener(_onWorkshopChanged);
    super.dispose();
  }
}