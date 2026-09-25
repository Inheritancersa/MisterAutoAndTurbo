namespace MisterAutoAndTurbo.Web.Models
{
    public class JobPart
    {
        public string Id { get; set; }
        public string JobCardId { get; set; }
        public string PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public bool CustomerSupplied { get; set; }
    }
}
