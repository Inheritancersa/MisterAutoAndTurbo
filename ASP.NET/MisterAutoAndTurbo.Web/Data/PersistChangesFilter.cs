using Microsoft.AspNetCore.Mvc.Filters;

namespace MisterAutoAndTurbo.Web.Data
{
    /// <summary>
    /// Keeps the staff app and the Flutter customer app in sync through Supabase.
    /// Tuned for speed: throttled refresh, selective table loads, no full save on every GET.
    /// </summary>
    public class PersistChangesFilter : IActionFilter
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PersistChangesFilter> _log;

        public PersistChangesFilter(ApplicationDbContext db, ILogger<PersistChangesFilter> log)
        {
            _db = db;
            _log = log;
        }

        /// <summary>
        /// Runs before each controller action. Refreshes the in-memory data from the database (so changes made by the Flutter customer app are visible) and applies customer-app updates, saving them if needed. Account pages are skipped. Failures are logged, not thrown.
        /// </summary>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
            // Auth / error pages should not pay the Supabase round-trip cost.
            if (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                // Throttled + selective refresh (Flutter-writable tables + occasional full load).
                AppDataStore.RefreshFromDatabase(_db);

                // Create staff notifications / advance jobs when the customer app changed data.
                if (AppDataStore.ApplyCustomerAppUpdates())
                    AppDataStore.SaveToDatabase(_db);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Could not refresh data from Supabase before the action.");
            }
        }

        /// <summary>
        /// Runs after each controller action. For successful non-GET/HEAD requests (create, edit, delete) the in-memory data is saved to the database. Account pages are skipped; save errors are logged and rethrown.
        /// </summary>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null) return;
            if (HttpMethods.IsGet(context.HttpContext.Request.Method) ||
                HttpMethods.IsHead(context.HttpContext.Request.Method)) return;

            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
            if (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                AppDataStore.SaveToDatabase(_db);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to save changes to the database.");
                throw;
            }
        }
    }
}
