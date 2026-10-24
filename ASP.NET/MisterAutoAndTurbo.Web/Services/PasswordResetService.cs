using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace MisterAutoAndTurbo.Web.Services
{
    /// <summary>Creates and checks 6-digit password-reset codes (kept in memory, hashed, 10 minute expiry, 5 attempts).</summary>
    // Stores one reset code per user in a thread-safe dictionary. Only an HMAC hash of the code is kept, never the code itself.
    public class PasswordResetService
    {
        // Limits: 5 wrong attempts, codes live 10 minutes, a new code may be requested every 30 seconds.
        private const int MaxAttempts = 5;
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ResendDelay = TimeSpan.FromSeconds(30);

        // One stored reset code: hash, expiry time, creation time and the number of failed attempts.
        private sealed record Entry(string CodeHash, DateTime ExpiresUtc, DateTime CreatedUtc, int Attempts);
        private readonly ConcurrentDictionary<string, Entry> _codes = new();
        private readonly byte[] _key;

        /// <summary>
        /// Reads the HMAC secret key from configuration (Email:RecoveryCodeSecret) with a fallback value.
        /// </summary>
        public PasswordResetService(IConfiguration config)
        {
            _key = Encoding.UTF8.GetBytes(config["Email:RecoveryCodeSecret"] ?? "change-me-recovery-secret");
        }

        /// <summary>
        /// Hashes the user id and code together with HMAC-SHA256 and returns the result as a hex string.
        /// </summary>
        private string HashCode(string userId, string code) =>
            Convert.ToHexString(new HMACSHA256(_key).ComputeHash(Encoding.UTF8.GetBytes(userId + ":" + code)));

        /// <summary>Returns a new code, or null if one was requested less than 30 seconds ago.</summary>
        // Generates a random 6-digit code, stores its hash with the expiry time, and returns the plain code to be e-mailed.
        // Returns null if a code was created in the last 30 seconds (resend throttle).
        public string? CreateCode(string userId)
        {
            if (_codes.TryGetValue(userId, out var old) && DateTime.UtcNow - old.CreatedUtc < ResendDelay)
                return null;
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            _codes[userId] = new Entry(HashCode(userId, code), DateTime.UtcNow.Add(Lifetime), DateTime.UtcNow, 0);
            return code;
        }

        /// <summary>
        /// Checks a code entered by the user. Fails when there is no code, it has expired, or too many attempts were made (the code is then removed). Comparison uses a constant-time check; a wrong code increases the attempt counter, a correct one is consumed (removed) and true is returned.
        /// </summary>
        public bool Verify(string userId, string? code)
        {
            if (!_codes.TryGetValue(userId, out var e)) return false;
            if (DateTime.UtcNow > e.ExpiresUtc || e.Attempts >= MaxAttempts) { _codes.TryRemove(userId, out _); return false; }

            var ok = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(e.CodeHash), Encoding.UTF8.GetBytes(HashCode(userId, (code ?? "").Trim())));
            if (!ok) { _codes[userId] = e with { Attempts = e.Attempts + 1 }; return false; }

            _codes.TryRemove(userId, out _);
            return true;
        }
    }
}
