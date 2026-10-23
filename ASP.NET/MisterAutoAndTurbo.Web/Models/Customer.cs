using System.ComponentModel.DataAnnotations;

namespace MisterAutoAndTurbo.Web.Models
{
    public class Customer
    {
        public string Id { get; set; } = string.Empty;
        public string UserAccountId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Phone number can only contain digits, spaces, and + - ( ).")]
        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? WorkAddress { get; set; }

        [Phone(ErrorMessage = "Enter a valid work phone number.")]
        [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Work phone can only contain digits, spaces, and + - ( ).")]
        public string? WorkPhone { get; set; }
    }
}
