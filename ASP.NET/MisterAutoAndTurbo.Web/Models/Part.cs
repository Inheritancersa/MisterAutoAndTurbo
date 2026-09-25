namespace MisterAutoAndTurbo.Web.Models
{
    public class Part
    {
        public string Id { get; set; }
        public string StockKeepingUnit { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int QuantityInStock { get; set; }
        public decimal UnitPrice { get; set; }
        public int ReorderLevel { get; set; }

    }
}
