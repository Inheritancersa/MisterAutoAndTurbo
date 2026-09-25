namespace MisterAutoAndTurbo.Web.Models
{
    public class JobCard
    {
        public string Id { get; set; }
        public string VehichleId { get; set; }
        public string CreatedById { get; set; }
        public string MechanicId { get; set; }
        public string Status { get; set; }
        public string Diagnosis { get; set; }
        public string RepairNotes { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
