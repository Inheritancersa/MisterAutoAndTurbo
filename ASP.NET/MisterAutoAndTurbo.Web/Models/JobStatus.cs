namespace MisterAutoAndTurbo.Web.Models
{
    /// <summary>
    /// Allowed job-card status values (matches SRS / sequence diagrams).
    /// </summary>
    public static class JobStatus
    {
        public const string Inspection = "Inspection";
        public const string Quotation = "Quotation";
        public const string InProgress = "In Progress";
        public const string QualityCheck = "Quality Check";
        public const string ReadyForCollection = "Ready for Collection";
        public const string Collected = "Collected";

        // All statuses in workflow order.
        public static readonly string[] All =
        {
            Inspection,
            Quotation,
            InProgress,
            QualityCheck,
            ReadyForCollection,
            Collected
        };
    }
}
