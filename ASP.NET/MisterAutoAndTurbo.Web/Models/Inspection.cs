namespace MisterAutoAndTurbo.Web.Models
{
    public class Inspection
    {
        public string Id { get; set; }
        public string JobCardId { get; set; }
        public string MechanicId { get; set; }
        public string Findings { get; set; }
        public string RecommendedAction { get; set; }
        public DateTime InspectedAt { get; set; }

    }
}
