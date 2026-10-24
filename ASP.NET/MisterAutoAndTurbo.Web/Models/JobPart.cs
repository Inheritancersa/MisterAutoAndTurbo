namespace MisterAutoAndTurbo.Web.Models
{
    public class JobPart
    /// <summary>
    /// A part used on a job card: quantity, unit price and whether the customer supplied it.
    /// </summary>
    {
        public string Id { get; set; }
        public string JobCardId { get; set; }
        public string PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public bool CustomerSupplied { get; set; }
    }
}
