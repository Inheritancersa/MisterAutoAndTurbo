namespace MisterAutoAndTurbo.Web.Models
{
    public class Quotation
    {
        public string Id { get; set; } = string.Empty;
        public string JobCardId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string VehicleId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal LabourCost { get; set; }
        public decimal PartsCost { get; set; }
        public decimal CustomerSuppliedDifference { get; set; }
        public decimal TotalCost { get; set; }
        /// <summary>Pending → Sent (to customer) → Approved | Rejected</summary>
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        /// <summary>When staff sent the quote to the customer (notification).</summary>
        public DateTime? SentAt { get; set; }
        /// <summary>When the customer approved or rejected.</summary>
        public DateTime? RespondedAt { get; set; }
    }
}
