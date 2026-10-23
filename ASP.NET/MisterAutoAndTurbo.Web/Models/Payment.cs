namespace MisterAutoAndTurbo.Web.Models
{
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

        public bool HasProof => ProofUrls.Count > 0;
    }
}
