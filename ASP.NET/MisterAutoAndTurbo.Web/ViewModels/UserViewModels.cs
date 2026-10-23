using System.ComponentModel.DataAnnotations;

namespace MisterAutoAndTurbo.Web.ViewModels
{
    public class UserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    public class UsersIndexViewModel
    {
        public string? Search { get; set; }
        public string? Role { get; set; }
        public int TotalUsers { get; set; }
        public List<UserListItemViewModel> Users { get; set; } = new();
    }

    public class UserFormViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Enter a username.")]
        [StringLength(40, MinimumLength = 3, ErrorMessage = "Username must be 3 to 40 characters.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter an email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Phone number can only contain digits, spaces, and + - ( ).")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Select a role.")]
        public string Role { get; set; } = string.Empty;

        // Only used when adding a user.
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
    }

    public class ResetPasswordViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Username { get; set; }

        [Required(ErrorMessage = "Enter a new password.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm the new password.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
