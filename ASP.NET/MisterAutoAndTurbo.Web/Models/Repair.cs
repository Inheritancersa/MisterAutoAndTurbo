namespace MisterAutoAndTurbo.Web.Models
{
    public class Repair
    {
        public string Id { get; set; }
        public string JobCardId { get; set; }
        public string MechanicId { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}
