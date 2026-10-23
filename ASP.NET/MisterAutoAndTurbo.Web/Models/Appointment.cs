namespace MisterAutoAndTurbo.Web.Models
{
    public class Appointment
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string VehicleId { get; set; } = string.Empty;
        /// <summary>Optional link to a job card (Job ID).</summary>
        public string JobCardId { get; set; } = string.Empty;
        public string CreatedById { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public string Status { get; set; } = string.Empty;
        /// <summary>When the appointment date was sent to the customer.</summary>
        public DateTime? SentAt { get; set; }

        /// <summary>Service type requested by the customer (e.g. Diagnostic, Repair).</summary>
        public string ServiceType { get; set; } = string.Empty;

        /// <summary>Customer notes / description of the work requested.</summary>
        public string Description { get; set; } = string.Empty;
    }
}
