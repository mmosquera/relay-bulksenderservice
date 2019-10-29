using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Configuration.Alerts;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace Relay.BulkSenderService.Processors.Errors
{
    public abstract class Error : IError
    {
        private readonly IConfiguration _configuration;

        public Error(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public DateTime Date { get; set; }
        public string Message { get; set; }

        public void Process()
        {
            throw new NotImplementedException();
        }

        public void SendErrorEmail(string file, AlertConfiguration alerts)
        {
            if (alerts != null
                && alerts.GetErrorAlert() != null
                && alerts.Emails.Count > 0)
            {
                var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
                smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

                var mailMessage = new MailMessage()
                {
                    Subject = alerts.GetErrorAlert().Subject,
                    From = new MailAddress("support@dopplerrelay.com", "Doppler Relay Support")
                };

                foreach (string email in alerts.Emails)
                {
                    mailMessage.To.Add(email);
                }

                string body = GetBody();

                if (string.IsNullOrEmpty(body))
                {
                    body = "Error processing file {{filename}}.";
                }

                mailMessage.Body = body.Replace("{{filename}}", Path.GetFileNameWithoutExtension(file));
                mailMessage.IsBodyHtml = true;

                try
                {
                    smtpClient.Send(mailMessage);
                }
                catch (Exception e)
                {
                    //_logger.Error($"Error trying to send error process email -- {e}");
                }
            }
        }

        protected abstract string GetBody();
    }
}
