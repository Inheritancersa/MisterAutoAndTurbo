namespace MisterAutoAndTurbo.Web.Models
{
    public class Quotation
    {
        public string Id { get; set; }
        public string JobCardId { get; set; }
        public decimal LabourCost { get; set; }
        public decimal PartsCost { get; set; }
        public decimal CustomerSuppliedDifference { get; set; }
        public decimal TotalCost { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
