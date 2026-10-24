namespace MisterAutoAndTurbo.Web.Models
{
    /// <summary>
    /// A service request made by a customer for one of their vehicles.
    /// </summary>
    public class ServiceRequest
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string VehicleId { get; set; } = string.Empty;
        public string RequestedService { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
