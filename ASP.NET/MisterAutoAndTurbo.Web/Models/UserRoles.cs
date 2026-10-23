namespace MisterAutoAndTurbo.Web.Models
{
    public static class UserRoles
    {
        public const string Manager = "Manager";
        public const string Receptionist = "Receptionist";
        public const string Mechanic = "Mechanic";

        public static readonly string[] All = { Manager, Receptionist, Mechanic };

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
