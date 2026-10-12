namespace MisterAutoAndTurbo.Web.Models
{
    public class Appointment
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string VehicleId { get; set; } = string.Empty;
        public string CreatedById { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
