namespace MisterAutoAndTurbo.Web.Models
{
    /// <summary>
    /// A message shown to a customer or staff user, with a type, read flag and an optional related record id (e.g. a quotation).
    /// </summary>
    public class Notification
    {
        public string Id { get; set; } = string.Empty;
        /// <summary>Customer id or staff user-account id who receives this.</summary>
        public string UserAccountId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        /// <summary>e.g. Quotation, JobUpdate, System</summary>
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        /// <summary>Related quotation id when Type is Quotation.</summary>
        public string? RelatedId { get; set; }
    }
}
