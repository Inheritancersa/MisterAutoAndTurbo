namespace MisterAutoAndTurbo.Web.Models
{
    public class Mechanic
    /// <summary>
    /// Mechanic profile linked to a user account (specialisation and status).
    /// </summary>
    {
        public string Id { get; set; }
        public string UderAccountId { get; set; }
        public string Specialisation { get; set; }
        public string Status { get; set; }
    }
}
