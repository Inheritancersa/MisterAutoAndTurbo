namespace MisterAutoAndTurbo.Web.Models
{
    public class Appointment
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string VehicleId { get; set; }
        public string CreatedById { get; set; }
        public DateTime ScheduledAt { get; set; }
        public string Status { get; set; }

    }
}
