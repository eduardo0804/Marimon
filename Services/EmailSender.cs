using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Marimon.Services
{
    // Interfaz extendida que soporta adjuntos
    public interface IEmailSenderWithAttachments : IEmailSender
    {
        Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, 
            byte[] attachmentData, string attachmentName, string contentType);
    }

    // Implementación que soporta adjuntos
    public class EmailSenderWithAttachments : IEmailSenderWithAttachments
    {
        private readonly EmailSettings _emailSettings;
        
        public EmailSenderWithAttachments(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }
        
        // Implementación del método estándar de IEmailSender
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await SendEmailInternalAsync(email, subject, htmlMessage, null);
        }
        
        // Implementación del método con soporte para adjuntos
        public async Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, 
            byte[] attachmentData, string attachmentName, string contentType)
        {
            if (attachmentData == null || string.IsNullOrEmpty(attachmentName))
                throw new ArgumentException("Attachment data and name must be provided");
            
            // Crear una lista de adjuntos con un solo elemento
            var attachments = new List<AttachmentInfo> 
            {
                new AttachmentInfo(attachmentData, attachmentName, contentType)
            };
            
            await SendEmailInternalAsync(email, subject, htmlMessage, attachments);
        }
        
        // Método interno para manejar ambos casos
        private async Task SendEmailInternalAsync(string email, string subject, string htmlMessage, 
            List<AttachmentInfo> attachments)
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
                    
                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_emailSettings.FromEmail, "Marimon Autopartes");
                        message.Subject = subject;
                        message.Body = htmlMessage;
                        message.IsBodyHtml = true;
                        message.To.Add(email);
                        
                        // Agregar adjuntos si existen
                        if (attachments != null && attachments.Count > 0)
                        {
                            foreach (var attachment in attachments)
                            {
                                var ms = new MemoryStream(attachment.Data);
                                var att = new Attachment(ms, attachment.Name, attachment.ContentType);
                                message.Attachments.Add(att);
                            }
                        }
                        
                        await client.SendMailAsync(message);
                    }
                }
            }
            catch (SmtpException ex)
            {
                throw new InvalidOperationException($"SMTP error sending email to {email}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error sending email: {ex.Message}", ex);
            }
        }
    }

    // Clase auxiliar para la información de adjuntos
    public class AttachmentInfo
    {
        public byte[] Data { get; }
        public string Name { get; }
        public string ContentType { get; }

        public AttachmentInfo(byte[] data, string name, string contentType)
        {
            Data = data;
            Name = name;
            ContentType = contentType;
        }
    }
}