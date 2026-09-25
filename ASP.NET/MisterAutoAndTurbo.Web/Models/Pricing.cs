namespace MisterAutoAndTurbo.Web.Models
{
    public class Pricing
    {
        public string Id { get; set; }
        public string PartId { get; set; }
        public string UpdatedById { get; set; }
        public decimal Price { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
    }
}
