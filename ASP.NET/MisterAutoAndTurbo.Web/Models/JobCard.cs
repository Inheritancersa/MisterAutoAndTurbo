namespace MisterAutoAndTurbo.Web.Models
{
    /// <summary>
    /// The central work record for a vehicle in the workshop. Links the customer, vehicle, creator and mechanic, and holds the status, diagnosis and repair notes.
    /// </summary>
    public class JobCard
    {
        public string Id { get; set; } = string.Empty;
        public string VehichleId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CreatedById { get; set; } = string.Empty;
        public string MechanicId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string RepairNotes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
