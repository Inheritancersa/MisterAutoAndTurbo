namespace MisterAutoAndTurbo.Web.Models
{
    public class UserAccount
    /// <summary>
    /// A login account for staff or customers: username, e-mail, phone, hashed password and role.
    /// </summary>
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
