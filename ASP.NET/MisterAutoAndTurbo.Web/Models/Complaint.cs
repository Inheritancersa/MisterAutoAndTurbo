namespace MisterAutoAndTurbo.Web.Models
{
    /*This class is NOT limited to the comments of unhappy customers.
     * This class serves to store the comments of happy customers as well as the complaints of customers 
     * who are not satisfied with the service provided by the company.
     */
    public class Complaint
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string JobCardId { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
