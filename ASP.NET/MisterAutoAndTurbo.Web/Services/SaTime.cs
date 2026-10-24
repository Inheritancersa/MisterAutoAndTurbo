namespace MisterAutoAndTurbo.Web.Services
{
    /// <summary>
    /// South African time helpers (SAST, UTC+2, no DST).
    /// System timestamps (CreatedAt, SentAt, mobile-app writes...) are stored in UTC;
    /// staff-typed wall-clock times (appointment / collection schedule) are stored as entered.
    /// Use ToSa() when DISPLAYING a UTC timestamp, FromSa() when saving a staff-entered one.
    /// </summary>
    public static class SaTime
    {
        // Resolved once: the South African time zone object.
        private static readonly TimeZoneInfo Zone = Find();

        /// <summary>
        /// Finds the South African time zone on Linux or Windows; falls back to a fixed UTC+2 zone if neither name exists.
        /// </summary>
        private static TimeZoneInfo Find()
        {
            foreach (var id in new[] { "Africa/Johannesburg", "South Africa Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { }
            }
            return TimeZoneInfo.CreateCustomTimeZone("SAST", TimeSpan.FromHours(2), "SAST", "SAST");
        }

        /// <summary>Current wall-clock time in South Africa.</summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

        /// <summary>UTC value -> South African wall-clock time.</summary>
        public static DateTime ToSa(this DateTime utc)
        {
            if (utc == default) return utc;
            var u = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(u, Zone);
        }

        public static DateTime? ToSa(this DateTime? utc) => utc.HasValue ? utc.Value.ToSa() : null;

        /// <summary>South African wall-clock time -> UTC (as an Unspecified-kind value for the DB).</summary>
        public static DateTime FromSa(DateTime sa)
        {
            if (sa == default) return sa;
            var u = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(sa, DateTimeKind.Unspecified), Zone);
            return DateTime.SpecifyKind(u, DateTimeKind.Unspecified);
        }
    }
}
