namespace MisterAutoAndTurbo.Web.Models
{
    /// <summary>
    /// A customer complaint about a job or vehicle. It can come from the mobile app, staff or phone, and is tracked from Open to Closed with staff notes.
    /// </summary>
    public class Complaint
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string JobCardId { get; set; } = string.Empty;
        public string VehicleId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>Open | In Progress | Resolved | Closed</summary>
        public string Status { get; set; } = "Open";
        /// <summary>Mobile | Staff | Phone</summary>
        public string Source { get; set; } = "Staff";
        public string StaffNotes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
