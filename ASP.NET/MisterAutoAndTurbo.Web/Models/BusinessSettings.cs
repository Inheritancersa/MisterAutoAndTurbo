namespace MisterAutoAndTurbo.Web.Models
{
    /// <summary>Workshop-wide settings edited on the Settings page (Manager only).</summary>
    public class BusinessSettings
    {
        /// <summary>Single settings row (always 1).</summary>
        public int Id { get; set; } = 1;

        public string BusinessName { get; set; } = "Mister Auto & Turbo";
        public string VatNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Currency { get; set; } = "ZAR";

        /// <summary>VAT as a percentage, e.g. 15 means 15%.</summary>
        public decimal VatRate { get; set; } = 15m;

        /// <summary>Currencies offered on the Settings page (code, display name).</summary>
        // Currencies available on the Settings page: code mapped to display name.
        public static readonly Dictionary<string, string> Currencies = new()
        {
            ["ZAR"] = "South African Rand (R)"
        };
    }
}
