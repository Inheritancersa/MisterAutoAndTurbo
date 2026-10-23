namespace MisterAutoAndTurbo.Web.Models
{
    public class VehicleCollection
    {
        public string Id { get; set; } = string.Empty;
        public string JobCardId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string VehicleId { get; set; } = string.Empty;
        public string ManagedById { get; set; } = string.Empty;
        /// <summary>Pending | Ready | Collected | Cancelled</summary>
        public string CollectionStatus { get; set; } = "Pending";
        public DateTime ScheduledAt { get; set; }
        public DateTime? CollectedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
