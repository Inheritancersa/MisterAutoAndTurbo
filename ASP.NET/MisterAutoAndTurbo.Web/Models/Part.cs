namespace MisterAutoAndTurbo.Web.Models
{
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
