using Relay.BulkSenderService.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace Relay.BulkSenderService.Classes
{
    public class MailSender
    {
        private readonly IConfiguration _configuration;
        public MailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SendEmail(string from, string fromName, List<string> emails, string subject, string body, List<string> attachments = null)
        {
            var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
            smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

            var mailMessage = new MailMessage()
            {
                Subject = subject,
                From = new MailAddress(from, fromName),
                Body = body,
                IsBodyHtml = true
            };

            foreach (string email in emails)
            {
                mailMessage.To.Add(email);
            }

            if (attachments != null)
            {
                foreach (string attachment in attachments)
                {
                    mailMessage.Attachments.Add(new Attachment(attachment)
                    {
                        Name = Path.GetFileName(attachment)
                    });
                }
            }

            smtpClient.Send(mailMessage);
        }
    }
}
