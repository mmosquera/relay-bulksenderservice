using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;

namespace Relay.BulkSenderService.Processors
{
    public class SMTPProcessor : Processor
    {
        public SMTPProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration) { }

        protected override string Process(IUserConfiguration user, string localFileName, ProcessResult result)
        {
            if (string.IsNullOrEmpty(localFileName))
            {
                return null;
            }

            var recipients = new List<SMTPRecipient>();
            var resultsFile = new StringBuilder();

            string fileName = Path.GetFileName(localFileName);

            var filePathHelper = new FilePathHelper(_configuration, user.Name);
            string resultsFileName = $@"{filePathHelper.GetResultsFilesFolder()}\{fileName.Replace(".processing", ".sent")}";

            try
            {
                _logger.Debug($"Read file {localFileName}");

                using (StreamReader reader = new StreamReader(localFileName))
                {
                    string headers = reader.ReadLine();

                    AddExtraHeaders(resultsFile, headers, ((UserSMTPConfiguration)user).FieldSeparator);

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();

                        var recipient = CreateRecipientFromString(line, ((UserSMTPConfiguration)user).TemplateFilePath, ((UserSMTPConfiguration)user).AttachFilePath, ((UserSMTPConfiguration)user).FieldSeparator);

                        recipients.Add(recipient);

                        if (recipients.Count == _configuration.BulkEmailCount)
                        {
                            SendRecipientList(recipients, ((UserSMTPConfiguration)user).SmtpUser, ((UserSMTPConfiguration)user).SmtpPass, resultsFileName, resultsFile, ((UserSMTPConfiguration)user).FieldSeparator);

                            resultsFile.Clear();
                        }
                    }

                    SendRecipientList(recipients, ((UserSMTPConfiguration)user).SmtpUser, ((UserSMTPConfiguration)user).SmtpPass, resultsFileName, resultsFile, ((UserSMTPConfiguration)user).FieldSeparator);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"ERROR on files processing --- {e}");
            }

            return resultsFileName;
        }

        private void SendRecipientList(List<SMTPRecipient> recipients, string smtpUser, string smtpPass, string resultsFileName, StringBuilder resultsFile, char separator)
        {
            foreach (SMTPRecipient recipient in recipients)
            {
                if (!recipient.HasError)
                {
                    SendMessage(recipient, smtpUser, smtpPass, separator);

                    Thread.Sleep(_configuration.DeliveryInterval);
                }

                resultsFile.Append(recipient.ResultLine);
            }

            using (StreamWriter sw = new StreamWriter(resultsFileName, true))
            {
                sw.Write(resultsFile.ToString());
            }
        }

        private void SendMessage(SMTPRecipient recipient, string smtpUser, string smtpPass, char separator)
        {
            var message = new MailMessage();

            message.From = new MailAddress(recipient.FromEmail, recipient.FromName);
            message.To.Add(new MailAddress(recipient.ToEmail, recipient.ToName));
            message.Subject = recipient.Subject;
            message.IsBodyHtml = true;
            message.Body = recipient.Body;

            if (!string.IsNullOrEmpty(recipient.AttachFileName))
            {
                var attachment = new Attachment(recipient.AttachFileName);
                attachment.Name = Path.GetFileName(recipient.AttachFileName);
                message.Attachments.Add(attachment);
            }

            var client = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
            client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            try
            {
                _logger.Debug($"Send message {message.Subject} from {message.From.Address} to {message.To[0].Address}");
                client.Send(message);
                recipient.AddSentResult(separator, "Send OK");
            }
            catch (Exception e)
            {
                recipient.AddSentResult(separator, "Send Fail");
                _logger.Error($"ERROR trying to send message --- {e}");
            }
        }

        private SMTPRecipient CreateRecipientFromString(string line, string templateFilePath, string attachFilePath, char separator)
        {
            var recipient = new SMTPRecipient();

            _logger.Debug("Create message to send");

            string[] mailArray = line.Split(separator);

            if (mailArray.Length != 8)
            {
                string error = "Invalid data to create message";

                recipient.HasError = true;
                recipient.AddProcessedResult(line, separator, error);

                _logger.Error(error);

                return null;
            }

            try
            {
                recipient.FromEmail = mailArray[0];
                recipient.FromName = mailArray[1];
                recipient.ToEmail = mailArray[2];
                recipient.ToName = mailArray[3];
                recipient.Subject = mailArray[4];
                recipient.Body = mailArray[5];

                if (!string.IsNullOrEmpty(mailArray[6]))
                {
                    var templateFile = $"{templateFilePath}/{mailArray[6]}";
                    if (File.Exists(templateFile))
                    {
                        using (StreamReader templateReader = new StreamReader(templateFile))
                        {
                            recipient.Body = templateReader.ReadToEnd();
                        }
                    }
                    else
                    {
                        _logger.Error($"Template file doesn't exists {templateFile}");
                    }
                }

                if (!string.IsNullOrEmpty(mailArray[7]))
                {
                    var attachFileName = $"{attachFilePath}/{mailArray[7]}";
                    if (File.Exists(attachFileName))
                    {
                        recipient.AttachFileName = attachFileName;
                    }
                    else
                    {
                        _logger.Error($"Attach file doesn't exists {attachFileName}");
                    }
                }

                recipient.AddProcessedResult(line, separator, Constants.PROCESS_RESULT_OK);
            }
            catch (Exception e)
            {
                recipient.HasError = true;
                recipient.AddProcessedResult(line, separator, "Error creating message");
                _logger.Error($"ERROR creating message --- {e}");
            }

            return recipient;
        }

        private void AddExtraHeaders(StringBuilder resultsFile, string headers, char separator)
        {
            resultsFile.AppendLine($"{headers}{separator}{Constants.HEADER_PROCESS_RESULT}{separator}{Constants.HEADER_DELIVERY_RESULT}");
        }

        protected override List<string> GetAttachments(string file, string userName)
        {
            return new List<string>();
        }

        protected override string GetBody(string file, IUserConfiguration user, ProcessResult result)
        {
            return null;
        }
    }
}
