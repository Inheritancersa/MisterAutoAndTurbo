namespace MisterAutoAndTurbo.Web.Models
{
    public class ServiceRequest
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string VehicleId { get; set; }
        public string RequestedService { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
