import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart';
//import '../models/notification_item.dart';
import '../utils/helpers.dart';
import 'workshop_viewmodel.dart';

/// State of the Proof Of Payment screen.
/// Supports multiple documents with a combined maximum size of 10 MB.
class ProofOfPaymentViewModel extends ChangeNotifier {
  ProofOfPaymentViewModel(this._workshop);

  static const int maxTotalBytes = 10 * 1024 * 1024;

  final WorkshopViewModel _workshop;
  final List<PlatformFile> _files = [];
  bool _done = false;
  bool _picking = false;
  bool _disposed = false;

  List<PlatformFile> get files => List.unmodifiable(_files);
  bool get done => _done;
  bool get picking => _picking;

  int get totalBytes => _files.fold(0, (total, file) => total + file.size);
  int get remainingBytes => maxTotalBytes - totalBytes;
  bool get isFull => totalBytes >= maxTotalBytes;

  @override
  void dispose() {
    _disposed = true;
    super.dispose();
  }

  // The file dialog is asynchronous, so the screen may be gone by the time it
  // closes. Never notify a disposed ChangeNotifier.
  void _notify() {
    if (!_disposed) notifyListeners();
  }

  /// Opens the file picker and adds the chosen files.
  /// Returns a message to show the user, or null when there is nothing to say.
  Future<String?> pickFiles() async {
    if (_picking) return null;
    _picking = true;
    _notify();

    try {
      final result = await FilePicker.platform.pickFiles(
        type: FileType.custom,
        allowedExtensions: const ['pdf', 'jpg', 'jpeg', 'png'],
        allowMultiple: true,
        withData: true,
      );

      if (_disposed || result == null || result.files.isEmpty) return null;

      final newFiles = <PlatformFile>[];
      var addedBytes = 0;
      var duplicateCount = 0;

      for (final file in result.files) {
        bool same(PlatformFile other) =>
            other.name == file.name && other.size == file.size;

        if (_files.any(same) || newFiles.any(same)) {
          duplicateCount++;
          continue;
        }
        newFiles.add(file);
        addedBytes += file.size;
      }

      if (newFiles.isEmpty) {
        return duplicateCount > 0
            ? 'These files have already been selected'
            : null;
      }

      if (totalBytes + addedBytes > maxTotalBytes) {
        return 'Selection exceeds 10 MB. '
            'You have ${fmtSize(remainingBytes)} remaining.';
      }

      _files.addAll(newFiles);
      return duplicateCount > 0 ? 'Duplicate files were skipped' : null;
    } catch (e) {
      return 'Could not open files: $e';
    } finally {
      _picking = false;
      _notify();
    }
  }

  void removeFile(int index) {
    _files.removeAt(index);
    _notify();
  }

  /// Validates and "uploads" the documents. Returns an error message, or null
  /// on success (the screen then shows the confirmation).
  Future<String?> submit() async {
    if (_files.isEmpty) return 'Choose at least one document first';
    if (totalBytes > maxTotalBytes) {
      return 'Combined file size cannot exceed 10 MB';
    }
    // Upload the first file (or extend later for multi-file).
    final file = _files.first;
    final bytes = file.bytes;
    if (bytes == null) {
      return 'Could not read file bytes. Try picking the file again.';
    }
    final err = await _workshop.submitProofOfPayment(
      fileName: file.name,
      bytes: bytes,
    );
    if (err != null) return err;
    _done = true;
    _notify();
    return null;
  }
}
