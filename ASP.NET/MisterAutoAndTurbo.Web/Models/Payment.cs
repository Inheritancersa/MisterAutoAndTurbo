namespace MisterAutoAndTurbo.Web.Models
{
    public class Payment
    {
        public string Id { get; set; }
        public string QuotationId { get; set; }
        public string RecordedById { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public string ProofOfPaymentUrl { get; set; }
        public string Status { get; set; }
        public DateTime PaidAt { get; set; }

    }
}
