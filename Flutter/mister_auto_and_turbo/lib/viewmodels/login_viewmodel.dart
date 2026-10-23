import 'package:flutter/foundation.dart';
import 'auth_viewmodel.dart';

/// State of the Log In screen: the error message shown under the form.
class LoginViewModel extends ChangeNotifier {
  LoginViewModel(this._auth);

  final AuthViewModel _auth;
  String? _error;
  bool _busy = false;

  String? get error => _error;
  bool get isBusy => _busy;

  /// Tries to log in. Returns true on success; otherwise [error] is set.
  Future<bool> login(String email, String password) async {
    _busy = true;
    _error = null;
    notifyListeners();

    _error = await _auth.login(email.trim(), password);

    _busy = false;
    notifyListeners();
    return _error == null;
  }

  void clearError() {
    if (_error == null) return;
    _error = null;
    notifyListeners();
  }
}
