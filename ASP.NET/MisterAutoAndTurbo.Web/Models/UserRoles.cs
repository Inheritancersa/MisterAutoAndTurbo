namespace MisterAutoAndTurbo.Web.Models
{
    /// <summary>
    /// Role names used for authorization (Manager, Receptionist, Mechanic) and a helper for role pills.
    /// </summary>
    public static class UserRoles
    {
        public const string Manager = "Manager";
        public const string Receptionist = "Receptionist";
        public const string Mechanic = "Mechanic";

        public static readonly string[] All = { Manager, Receptionist, Mechanic };

        // Maps a role to the CSS class that colours its pill in the UI.
        /// <summary>CSS class for the coloured role pill.</summary>
        public static string PillClass(string? role) => role switch
        {
            Manager => "pill-blue",
            Receptionist => "pill-amber",
            Mechanic => "pill-green",
            _ => ""
        };
    }
}
