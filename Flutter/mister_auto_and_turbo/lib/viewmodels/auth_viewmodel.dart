import 'package:flutter/foundation.dart';
import 'package:supabase_flutter/supabase_flutter.dart';
import '../models/customer.dart';
import '../repositories/customer_repository.dart';
import '../services/supabase_service.dart';

/// Authentication for the customer app.
///
/// Uses **Supabase Auth** (email + password). On first sign-up a row is also
/// created in the `Customers` table so the staff MVC app can see the customer.
///
/// Staff accounts (Manager / Receptionist / Mechanic) stay in the MVC app and
/// use the separate `UserAccounts` table with ASP.NET password hashing.
class AuthViewModel extends ChangeNotifier {
  AuthViewModel({CustomerRepository? repository})
      : _repo = repository ?? CustomerRepository() {
    _restoreSession();
    SupabaseService.auth.onAuthStateChange.listen((data) {
      if (data.event == AuthChangeEvent.signedOut) {
        _customer = null;
        notifyListeners();
      }
    });
  }

  final CustomerRepository _repo;
  Customer? _customer;
  bool _busy = false;
  String? _error;

  Customer? get customer => _customer;
  bool get isLoggedIn => _customer != null;
  bool get isBusy => _busy;
  String? get error => _error;
  String get greeting => 'Hello, ${_customer?.firstName ?? '...'}';

  Future<void> _restoreSession() async {
    try {
      final session = SupabaseService.auth.currentSession;
      if (session == null) return;
      final user = SupabaseService.auth.currentUser;
      if (user == null) return;
      _customer = await _ensureCustomerProfile(user);
      notifyListeners();
    } catch (e) {
      debugPrint('_restoreSession failed: $e');
    }
  }

  /// Returns null on success, otherwise an error message.
  Future<String?> login(String email, String password) async {
    _busy = true;
    _error = null;
    notifyListeners();

    try {
      final res = await SupabaseService.auth.signInWithPassword(
        email: email.trim(),
        password: password,
      );
      if (res.user == null) {
        _error = 'Login failed. Please try again.';
        return _error;
      }

      // Always load (or create) a real Customers row. Never invent a local-only
      // Customer with the auth UUID as Id — that breaks Vehicles RLS because
      // current_customer_id() looks up Customers by UserAccountId/Email.
      _customer = await _ensureCustomerProfile(res.user!);

      // Force a fresh re-fetch of everything downstream so a logout→login
      // cycle does not show stale data. The WorkshopViewModel listens to this
      // ChangeNotifier and will call refresh() when isLoggedIn flips true.
      return null;
    } on AuthException catch (e) {
      _error = _friendlyAuthError(e);
      return _error;
    } catch (e) {
      debugPrint('login error: $e');
      _error = 'Could not reach the server. Check your connection.';
      return _error;
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  /// Creates an Auth user + Customers row when a session is available.
  /// User is NOT auto-logged-in (signed out after success).
  ///
  /// If email confirmation is required, Supabase returns no session after
  /// signUp — in that case the Customers row is created on the first login
  /// via [_ensureCustomerProfile] using user_metadata from signUp.
  Future<String?> register(Customer c, String password) async {
    _busy = true;
    _error = null;
    notifyListeners();

    try {
      if (password.length < 6) {
        _error = 'Password must be at least 6 characters.';
        return _error;
      }

      final res = await SupabaseService.auth.signUp(
        email: c.email.trim(),
        password: password,
        data: {
          'full_name': c.fullName,
          'phone': c.phone,
        },
      );

      final user = res.user;
      if (user == null) {
        _error = 'Registration failed. Please try again.';
        return _error;
      }

      // Only insert when we have an authenticated session (confirm-email off).
      // When confirm-email is on, session is null and RLS blocks the insert;
      // the profile is created on first successful login instead.
      if (res.session != null) {
        try {
          await _repo.createCustomerProfile(
            authUserId: user.id,
            fullName: c.fullName,
            email: c.email.trim(),
            phone: c.phone,
          );
        } catch (e) {
          // Profile may already exist, or insert failed — login will ensure it.
          debugPrint('createCustomerProfile during register: $e');
        }
        await SupabaseService.auth.signOut();
      }

      return null;
    } on AuthException catch (e) {
      _error = _friendlyAuthError(e);
      return _error;
    } catch (e) {
      debugPrint('register error: $e');
      _error = 'Could not create account: $e';
      return _error;
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  /// Loads the Customers row for [user], or inserts one if missing.
  Future<Customer> _ensureCustomerProfile(User user) async {
    final existing = await _repo.fetchCurrentCustomer();
    if (existing != null) return existing;

    final meta = user.userMetadata ?? {};
    final fullName = (meta['full_name'] as String?)?.trim().isNotEmpty == true
        ? meta['full_name'] as String
        : (user.email ?? 'Customer').split('@').first;
    final phone = (meta['phone'] as String?) ?? '';

    return _repo.createCustomerProfile(
      authUserId: user.id,
      fullName: fullName,
      email: user.email ?? '',
      phone: phone,
    );
  }

  Future<void> logout() async {
    try {
      await SupabaseService.auth.signOut();
    } catch (e) {
      debugPrint('logout error: $e');
    }
    _customer = null;
    notifyListeners();
  }

  /// Asks Supabase to email a verification code straight away.
  /// (The "Reset Password" email template in Supabase must contain {{ .Token }}.)
  Future<String?> sendPasswordReset(String email) async {
    try {
      await SupabaseService.auth.resetPasswordForEmail(email.trim());
      return null;
    } on AuthException catch (e) {
      return _friendlyAuthError(e);
    } catch (_) {
      return 'Could not send reset email. Check your connection.';
    }
  }


  /// Checks the emailed code and, if it is right, sets the new password.
  /// Returns null on success, otherwise an error message.
  Future<String?> resetPasswordWithCode({
    required String email,
    required String code,
    required String newPassword,
  }) async {
    if (newPassword.length < 6) return 'Password must be at least 6 characters.';
    try {
      await SupabaseService.auth.verifyOTP(
        email: email.trim(),
        token: code.trim(),
        type: OtpType.recovery,
      );
      await SupabaseService.auth.updateUser(UserAttributes(password: newPassword));
      // Make the customer log in with the new password.
      await SupabaseService.auth.signOut();
      _customer = null;
      notifyListeners();
      return null;
    } on AuthException catch (e) {
      final m = e.message.toLowerCase();
      if (m.contains('expired') || m.contains('invalid')) {
        return 'That code is invalid or has expired. Request a new one.';
      }
      return _friendlyAuthError(e);
    } catch (_) {
      return 'Could not reset password. Check your connection.';
    }
  }

  /// Updates name, phone and address on the Customers row.
  Future<String?> updateProfile({
    required String fullName,
    required String phone,
    required String address,
  }) async {
    final current = _customer;
    if (current == null) return 'Not logged in';
    _busy = true;
    _error = null;
    notifyListeners();
    try {
      final updated = await _repo.updateCustomerProfile(
        customerId: current.id,
        fullName: fullName.trim(),
        phone: phone.trim(),
        address: address.trim(),
      );
      _customer = updated;
      return null;
    } catch (e) {
      debugPrint('updateProfile error: $e');
      _error = 'Could not update profile: $e';
      return _error;
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  String _friendlyAuthError(AuthException e) {
    final msg = e.message.toLowerCase();
    if (msg.contains('invalid login') || msg.contains('invalid credentials')) {
      return 'Incorrect email or password.';
    }
    if (msg.contains('already registered') ||
        msg.contains('already been registered')) {
      return 'An account with this email already exists. Please log in.';
    }
    if (msg.contains('email not confirmed')) {
      return 'Please confirm your email before logging in.';
    }
    return e.message;
  }
}
