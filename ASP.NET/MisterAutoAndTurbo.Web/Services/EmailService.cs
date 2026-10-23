using System.Net;
using System.Net.Mail;

namespace MisterAutoAndTurbo.Web.Services
{
    public interface IEmailService
    {
        /// <summary>Sends the message right away (the call completes once the SMTP server accepted it).</summary>
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }

    /// <summary>Sends mail through the SMTP server configured in the "Email" section of appsettings.json.</summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public SmtpEmailService(IConfiguration config) => _config = config;

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var s = _config.GetSection("Email");
            var host = s["SmtpHost"] ?? throw new InvalidOperationException("Email:SmtpHost is not configured.");
            var port = int.TryParse(s["SmtpPort"], out var p) ? p : 587;
            var user = s["SmtpUser"];
            var pass = s["SmtpPass"];
            var from = s["FromEmail"] ?? user ?? throw new InvalidOperationException("Email:FromEmail is not configured.");
            var fromName = s["FromName"] ?? "Mister Auto And Turbo";

            using var message = new MailMessage { From = new MailAddress(from, fromName), Subject = subject, Body = htmlBody, IsBodyHtml = true };
            message.To.Add(toEmail);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000,
                Credentials = string.IsNullOrEmpty(user) ? null : new NetworkCredential(user, pass)
            };
            await client.SendMailAsync(message);
        }
    }
}
