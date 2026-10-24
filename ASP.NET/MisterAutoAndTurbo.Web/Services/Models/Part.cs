namespace MisterAutoAndTurbo.Web.Models
{
    /// <summary>
    /// A stock item in the parts inventory. Stock at or below ReorderLevel means the part should be re-ordered.
    /// </summary>
    public class Part
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int QuantityInStock { get; set; }
        public decimal UnitPrice { get; set; }
        public int ReorderLevel { get; set; }
    }
}
