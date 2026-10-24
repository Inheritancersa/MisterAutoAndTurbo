import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'routes/route_manager.dart';
import 'services/supabase_service.dart';
import 'theme.dart';
import 'viewmodels/auth_viewmodel.dart';
import 'viewmodels/workshop_viewmodel.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Connect to the same Supabase project used by the ASP.NET MVC staff app.
  // Configure URL + anon key in lib/config/supabase_config.dart first.
  try {
    await SupabaseService.initialize();
  } catch (e) {
    // Still launch so the developer can see the error on the login screen.
    debugPrint('Supabase init failed: $e');
  }

  runApp(const MisterAutoApp());
}

/// Mister Auto & Turbo – customer app (Flutter).
/// Screens follow the Figma "customer-proto" file.
/// Data is stored in Supabase (PostgreSQL) shared with the staff web app.
///
/// Architecture: MVVM with Provider.
///   models/        -> plain data classes
///   repositories/  -> Supabase data access
///   viewmodels/    -> ChangeNotifier classes (state + logic)
///   views/         -> screens (widgets that only draw the UI)
///   routes/        -> RouteManager: every named route in one place
class MisterAutoApp extends StatelessWidget {
  const MisterAutoApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthViewModel()),
        ChangeNotifierProvider(
          create: (context) => WorkshopViewModel(context.read<AuthViewModel>()),
        ),
      ],
      child: MaterialApp(
        title: 'Mister Auto & Turbo',
        debugShowCheckedModeBanner: false,
        theme: buildTheme(),
        initialRoute: RouteManager.splash,
        onGenerateRoute: RouteManager.generateRoute,
      ),
    );
  }
}
