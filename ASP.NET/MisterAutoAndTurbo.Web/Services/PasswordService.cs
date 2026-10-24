using Microsoft.AspNetCore.Identity;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Services
{
    /// <summary>
    /// Hashes passwords with ASP.NET's built-in PasswordHasher so they are not stored as plain text.
    /// The original demo accounts (manager, reception, mechanic) still hold plain-text passwords;
    /// Verify accepts those too, until each one is reset from the Users page.
    /// </summary>
    public static class PasswordService
    {
        // ASP.NET Identity hasher (PBKDF2 with a random salt).
        private static readonly PasswordHasher<UserAccount> Hasher = new();

        // ASP.NET Identity v3 hashes are base64 text that always begins with "AQAAAA".
        private static bool LooksHashed(string? value) =>
            !string.IsNullOrEmpty(value) && value.StartsWith("AQAAAA", StringComparison.Ordinal);

        /// <summary>
        /// Hashes a plain password for storing in UserAccount.PasswordHash.
        /// </summary>
        public static string Hash(string password) => Hasher.HashPassword(new UserAccount(), password);

        /// <summary>
        /// Checks a supplied password against the stored value. Old demo accounts with plain-text passwords are compared directly; hashed values are verified with the hasher.
        /// </summary>
        public static bool Verify(string? stored, string supplied)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            if (!LooksHashed(stored)) return stored == supplied;   // old plain-text demo account

            return Hasher.VerifyHashedPassword(new UserAccount(), stored, supplied) != PasswordVerificationResult.Failed;
        }

        /// <summary>Returns an error message if the password is too weak, otherwise null.</summary>
        // Password rules: at least 8 characters with at least one letter and one digit.
        public static string? Validate(string? password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return "Password must be at least 8 characters.";
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                return "Password must contain at least one letter and one number.";
            return null;
        }
    }
}
