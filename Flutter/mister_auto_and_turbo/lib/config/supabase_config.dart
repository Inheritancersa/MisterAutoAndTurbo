/// Supabase project configuration for the Mister Auto & Turbo customer app.
///
/// 1. Open your Supabase project dashboard → Settings → API
/// 2. Copy **Project URL** and the **anon public** key (never the service_role key).
/// 3. Paste them below.
///
/// Project ref from the MVC connection string: `fexkhhdfjummpkcdoqcm`
class SupabaseConfig {
  SupabaseConfig._();

  /// e.g. https://fexkhhdfjummpkcdoqcm.supabase.co
  static const String url = 'https://fexkhhdfjummpkcdoqcm.supabase.co';

  /// Paste your anon (public) key here. Safe to ship in the client app when
  /// Row Level Security policies are enabled on every table.
  static const String anonKey =
      'sb_publishable_-Dg_V3x89WB1H7_JtnYuBQ_0efkEf4I';

  static bool get isConfigured =>
      anonKey.isNotEmpty &&
      anonKey != 'YOUR_SUPABASE_ANON_KEY_HERE' &&
      url.startsWith('https://');
}
