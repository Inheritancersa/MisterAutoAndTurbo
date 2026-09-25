namespace MisterAutoAndTurbo.Web.Models
{
    public class VehicleCollection
    {
        public string Id { get; set; }
        public string JobCardId { get; set; }
        public string ManagedById { get; set; }
        public string CollectionStatus { get; set; }
        public DateTime ScheduledAt { get; set; }
        public DateTime CollectedAt { get; set; }
    }
}
