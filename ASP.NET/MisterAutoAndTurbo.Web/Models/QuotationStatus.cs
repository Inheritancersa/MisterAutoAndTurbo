namespace MisterAutoAndTurbo.Web.Models
{
    public static class QuotationStatus
    {
        public const string Pending = "Pending";
        public const string Sent = "Sent";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";

        public static readonly string[] All = { Pending, Sent, Approved, Rejected };
    }
}
