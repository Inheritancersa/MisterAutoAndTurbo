import 'package:supabase_flutter/supabase_flutter.dart';
import '../config/supabase_config.dart';

/// Thin wrapper around the global Supabase client.
/// Call [SupabaseService.initialize] once from [main] before [runApp].
class SupabaseService {
  SupabaseService._();

  static SupabaseClient get client => Supabase.instance.client;

  static GoTrueClient get auth => client.auth;

  static bool get isInitialized {
    try {
      // Accessing instance throws if initialize() was never called.
      return Supabase.instance.client.auth.currentSession != null || true;
    } catch (_) {
      return false;
    }
  }

  static Future<void> initialize() async {
    if (!SupabaseConfig.isConfigured) {
      throw StateError(
        'Supabase is not configured. Open lib/config/supabase_config.dart '
        'and paste your Project URL and anon key from the Supabase dashboard '
        '(Settings → API).',
      );
    }

    await Supabase.initialize(
      url: SupabaseConfig.url,
      anonKey: SupabaseConfig.anonKey,
      authOptions: const FlutterAuthClientOptions(
        authFlowType: AuthFlowType.pkce,
      ),
    );
  }
}
