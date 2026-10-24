/// Central navigation / routing configuration for the entire app.
///
/// Every named route is declared here as a constant string.
/// generateRoute is passed to MaterialApp.onGenerateRoute and is the
/// single place that maps a route name (+ optional arguments) to the
/// corresponding screen widget, wrapping it with any required Providers.
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/repair_job.dart';
import '../models/vehicle.dart';
import '../viewmodels/auth_viewmodel.dart';
import '../viewmodels/book_service_viewmodel.dart';
import '../viewmodels/complaint_viewmodel.dart';
import '../viewmodels/login_viewmodel.dart';
import '../viewmodels/proof_of_payment_viewmodel.dart';
import '../viewmodels/vehicle_status_viewmodel.dart';
import '../viewmodels/workshop_viewmodel.dart';
import '../views/book_service_view.dart';
import '../views/complaints_view.dart';
import '../views/contact_view.dart';
import '../views/edit_profile_view.dart';
import '../views/dashboard_view.dart';
import '../views/forgot_password_view.dart';
import '../views/history_view.dart';
import '../views/login_view.dart';
import '../views/my_bookings_view.dart';
import '../views/notifications_view.dart';
import '../views/proof_of_payment_view.dart';
import '../views/quotation_view.dart';
import '../views/register_vehicle_view.dart';
import '../views/reset_password_view.dart';
import '../views/register_view.dart';
import '../views/splash_view.dart';
import '../views/vehicle_status_view.dart';

/// Arguments for the dynamic [RouteManager.vehicleStatus] route.
class VehicleStatusArguments {
  final Vehicle vehicle;
  final RepairJob? job;
  const VehicleStatusArguments({required this.vehicle, this.job});
}

/// Central control for ALL navigation in the app.
///
/// Analogy: RouteManager is the receptionist. A screen says the room (route
/// name) and the manager sends it to the right room (view).
///
/// * Static routes need no data.
/// * Dynamic routes receive data through `arguments`:
///   login takes an optional `String` email, vehicleStatus takes a
///   VehicleStatusArguments.
///
/// Screens that have their own ViewModel get it here, wrapped around the view
/// with a ChangeNotifierProvider. The ViewModel is created when the route is
/// pushed and disposed when the route is removed.
class RouteManager {
  RouteManager._();

  // Static route names.
  static const String splash = '/';
  static const String login = '/login';
  static const String register = '/register';
  static const String forgotPassword = '/forgot-password';
  static const String resetPassword = '/reset-password';
  static const String dashboard = '/dashboard';
  static const String registerVehicle = '/register-vehicle';
  static const String bookService = '/book-service';
  static const String myBookings = '/my-bookings';
  static const String quotation = '/quotation';
  static const String history = '/history';
  static const String notifications = '/notifications';
  static const String complaints = '/complaints';
  static const String proofOfPayment = '/proof-of-payment';
  static const String contact = '/contact';
  static const String editProfile = '/edit-profile';

  // Dynamic route name (needs arguments).
  static const String vehicleStatus = '/vehicle-status';

  static Route<dynamic> generateRoute(RouteSettings settings) {
    switch (settings.name) {
      case splash:
        return _page(settings, const SplashView());

      case login:
        // Dynamic: an optional email to pre-fill after registering.
        final email = settings.arguments as String?;
        return _page(
          settings,
          ChangeNotifierProvider(
            create: (context) => LoginViewModel(context.read<AuthViewModel>()),
            child: LoginView(initialEmail: email),
          ),
        );

      case register:
        return _page(settings, const RegisterView());

      case forgotPassword:
        return _page(settings, const ForgotPasswordView());

      case resetPassword:
        return _page(settings, ResetPasswordView(email: settings.arguments as String? ?? ''));

      case dashboard:
        return _page(settings, const DashboardView());

      case registerVehicle:
        return _page(settings, const RegisterVehicleView());

      case vehicleStatus:
        // Dynamic: the vehicle (and its repair job, if any) to display.
        final args = settings.arguments as VehicleStatusArguments;
        return _page(
          settings,
          ChangeNotifierProvider(
            create: (context) => VehicleStatusViewModel(
              context.read<WorkshopViewModel>(),
              vehicle: args.vehicle,
              job: args.job,
            ),
            child: const VehicleStatusView(),
          ),
        );

      case bookService:
        return _page(
          settings,
          ChangeNotifierProvider(
            create: (context) =>
                BookServiceViewModel(context.read<WorkshopViewModel>()),
            child: const BookServiceView(),
          ),
        );

      case myBookings:
        return _page(settings, const MyBookingsView());

      case quotation:
        return _page(settings, const QuotationView());

      case history:
        return _page(settings, const HistoryView());

      case notifications:
        return _page(settings, const NotificationsView());

      case complaints:
        return _page(
          settings,
          ChangeNotifierProvider(
            create: (context) =>
                ComplaintViewModel(context.read<WorkshopViewModel>()),
            child: const ComplaintsView(),
          ),
        );

      case proofOfPayment:
        return _page(
          settings,
          ChangeNotifierProvider(
            create: (context) =>
                ProofOfPaymentViewModel(context.read<WorkshopViewModel>()),
            child: const ProofOfPaymentView(),
          ),
        );

      case contact:
        return _page(settings, const ContactView());

      case editProfile:
        return _page(settings, const EditProfileView());

      default:
        return _page(settings, _RouteNotFoundView(name: settings.name));
    }
  }

  static MaterialPageRoute<dynamic> _page(RouteSettings settings, Widget view) =>
      MaterialPageRoute<dynamic>(settings: settings, builder: (_) => view);
}

/// Shown instead of crashing when an unknown route name is requested.
class _RouteNotFoundView extends StatelessWidget {
  final String? name;
  const _RouteNotFoundView({this.name});
  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Page not found')),
        body: Center(child: Text('Route not found: ${name ?? 'unknown'}')),
      );
}
