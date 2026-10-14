namespace MisterAutoAndTurbo.Web.Models
{
    public class Customer
    {
        public string Id { get; set; } = string.Empty;
        public string UserAccountId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? WorkAddress { get; set; }
        public string? WorkPhone { get; set; }
    }
}
