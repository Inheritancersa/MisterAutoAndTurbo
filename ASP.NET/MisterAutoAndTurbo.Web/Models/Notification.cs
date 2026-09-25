namespace MisterAutoAndTurbo.Web.Models
{
    public class Notification
    {
        public string Id { get; set; }
        public string UserAccountId { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
