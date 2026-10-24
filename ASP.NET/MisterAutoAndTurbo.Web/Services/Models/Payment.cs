namespace MisterAutoAndTurbo.Web.Models
{
    /// <summary>
    /// A payment recorded against a quotation, with the method, status and optional proof-of-payment file URLs.
    /// </summary>
    public class Payment
    {
        public string Id { get; set; } = string.Empty;
        public string QuotationId { get; set; } = string.Empty;
        /// <summary>Customer paying (from quotation).</summary>
        public string CustomerId { get; set; } = string.Empty;
        public string RecordedById { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        /// <summary>Cash | Card | EFT | Other</summary>
        public string Method { get; set; } = string.Empty;
        /// <summary>
        /// Proof file URL(s). One upload = one URL. Multiple uploads over time
        /// are separate Payment rows; a single row may also hold several URLs
        /// separated by newlines or |.
        /// </summary>
        public string ProofOfPaymentUrl { get; set; } = string.Empty;
        /// <summary>Recorded | Confirmed | Refunded</summary>
        public string Status { get; set; } = "Recorded";
        public DateTime PaidAt { get; set; }

        /// <summary>All proof URLs on this payment row (supports multi-URL storage).</summary>
        // Splits ProofOfPaymentUrl on newline, pipe or semicolon, trims, removes duplicates and returns the list of proof URLs.
        public IReadOnlyList<string> ProofUrls
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ProofOfPaymentUrl))
                    return Array.Empty<string>();
                return ProofOfPaymentUrl
                    .Split(new[] { '\n', '\r', '|', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        // True when at least one proof file is attached.
        public bool HasProof => ProofUrls.Count > 0;
    }
}
