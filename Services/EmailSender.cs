using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Marimon.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        
        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }
        
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("Email destination cannot be null or empty", nameof(email));
            
            if (string.IsNullOrEmpty(subject))
                throw new ArgumentException("Subject cannot be null or empty", nameof(subject));
            try
            {
                using (var client = new SmtpClient())
                {
                    client.Host = _emailSettings.Host;
                    client.Port = _emailSettings.Port;
                    client.EnableSsl = _emailSettings.EnableSsl;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(
                        _emailSettings.FromEmail,
                        _emailSettings.Password
                    );
                    client.Timeout = 30000; // 30 segundos
                    var message = new MailMessage
                    {
                        From = new MailAddress(_emailSettings.FromEmail),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true
                    };
                    
                    message.To.Add(email);
                    
                    await client.SendMailAsync(message);
                }
            }
            catch (SmtpException ex)
            {
                // Loggear el error (usar ILogger si está disponible)
                throw new InvalidOperationException($"SMTP error sending email to {email}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Loggear otros errores
                throw new InvalidOperationException($"Unexpected error sending email: {ex.Message}", ex);
            }
        }
    }
}