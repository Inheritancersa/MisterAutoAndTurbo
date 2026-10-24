namespace MisterAutoAndTurbo.Web.Models
{
    public class Receptionist
    /// <summary>
    /// Receptionist profile linked to a user account (shift and status).
    /// </summary>
    {
        public string Id { get; set; }
        public string UderAccountId { get; set; }
        public string Shift { get; set; }
        public string Status { get; set; }

    }
}
