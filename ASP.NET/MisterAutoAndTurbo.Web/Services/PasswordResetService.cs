using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace MisterAutoAndTurbo.Web.Services
{
    /// <summary>Creates and checks 6-digit password-reset codes (kept in memory, hashed, 10 minute expiry, 5 attempts).</summary>
    public class PasswordResetService
    {
        private const int MaxAttempts = 5;
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ResendDelay = TimeSpan.FromSeconds(30);

        private sealed record Entry(string CodeHash, DateTime ExpiresUtc, DateTime CreatedUtc, int Attempts);
        private readonly ConcurrentDictionary<string, Entry> _codes = new();
        private readonly byte[] _key;

        public PasswordResetService(IConfiguration config)
        {
            _key = Encoding.UTF8.GetBytes(config["Email:RecoveryCodeSecret"] ?? "change-me-recovery-secret");
        }

        private string HashCode(string userId, string code) =>
            Convert.ToHexString(new HMACSHA256(_key).ComputeHash(Encoding.UTF8.GetBytes(userId + ":" + code)));

        /// <summary>Returns a new code, or null if one was requested less than 30 seconds ago.</summary>
        public string? CreateCode(string userId)
        {
            if (_codes.TryGetValue(userId, out var old) && DateTime.UtcNow - old.CreatedUtc < ResendDelay)
                return null;
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            _codes[userId] = new Entry(HashCode(userId, code), DateTime.UtcNow.Add(Lifetime), DateTime.UtcNow, 0);
            return code;
        }

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
