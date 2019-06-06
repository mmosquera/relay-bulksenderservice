using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
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

            string fileName = Path.GetFileName(localFileName);

            var filePathHelper = new FilePathHelper(_configuration, user.Name);
            string resultsFileName = GetResultsFileName(fileName, user);

            try
            {
                ITemplateConfiguration templateConfiguration = ((UserSMTPConfiguration)user).GetTemplateConfiguration(fileName);

                _logger.Debug($"Start to read file {localFileName}");

                using (StreamReader reader = new StreamReader(localFileName))
                {
                    string line = templateConfiguration.HasHeaders ? reader.ReadLine() : null;

                    string headers = GetHeaderLine(line, templateConfiguration);

                    AddExtraHeaders(resultsFileName, headers, templateConfiguration.FieldSeparator);

                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine();

                        string[] recipientArray = line.Split(templateConfiguration.FieldSeparator);

                        result.ProcessedCount++;

                        SMTPRecipient recipient = CreateRecipientFromString(recipientArray, line, ((UserSMTPConfiguration)user).TemplateFilePath, ((UserSMTPConfiguration)user).AttachmentsFolder, templateConfiguration.FieldSeparator);
                        FillRecipientAttachments(recipient, templateConfiguration, recipientArray, fileName, line, user, result);

                        recipients.Add(recipient);

                        if (recipients.Count == _configuration.BulkEmailCount)
                        {
                            SendRecipientList(recipients, ((UserSMTPConfiguration)user).SmtpUser, ((UserSMTPConfiguration)user).SmtpPass, resultsFileName, templateConfiguration.FieldSeparator);
                        }
                    }

                    SendRecipientList(recipients, ((UserSMTPConfiguration)user).SmtpUser, ((UserSMTPConfiguration)user).SmtpPass, resultsFileName, templateConfiguration.FieldSeparator);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"ERROR on files processing --- {e}");
            }

            return resultsFileName;
        }

        protected void SendRecipientList(List<SMTPRecipient> recipients, string smtpUser, string smtpPass, string resultsFileName, char separator)
        {
            using (StreamWriter sw = new StreamWriter(resultsFileName, true))
            {
                foreach (SMTPRecipient recipient in recipients)
                {
                    if (!recipient.HasError)
                    {
                        SendMessage(recipient, smtpUser, smtpPass, separator);

                        Thread.Sleep(_configuration.DeliveryInterval);
                    }
                    sw.WriteLine(recipient.ResultLine);
                    sw.Flush();
                }
            }
        }

        protected void SendMessage(SMTPRecipient recipient, string smtpUser, string smtpPass, char separator)
        {
            var message = new MailMessage()
            {
                From = new MailAddress(recipient.FromEmail, recipient.FromName),
                Subject = recipient.Subject,
                IsBodyHtml = true,
                Body = recipient.Body
            };

            message.To.Add(new MailAddress(recipient.ToEmail, recipient.ToName));

            foreach (string fileName in recipient.Attachments)
            {
                var attachment = new Attachment(fileName)
                {
                    Name = Path.GetFileName(fileName)
                };

                message.Attachments.Add(attachment);
            }

            var client = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
            client.Credentials = new NetworkCredential(smtpUser, smtpPass);

            try
            {
                client.Send(message);
                recipient.AddSentResult(separator, "Send OK");
            }
            catch (Exception e)
            {
                recipient.AddSentResult(separator, "Send Fail");
                _logger.Error($"ERROR trying to send message --- {e}");
            }
        }

        protected SMTPRecipient CreateRecipientFromString(string[] mailArray, string line, string templateFilePath, string attachFilePath, char separator)
        {
            var recipient = new SMTPRecipient();

            if (mailArray.Length != 8)
            {
                string error = "Invalid data to create message";

                recipient.HasError = true;
                recipient.AddProcessedResult(line, separator, error);

                _logger.Error(error);

                return recipient;
            }

            try
            {
                // TODO: fix recipient without email
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

        protected virtual void FillRecipientAttachments(SMTPRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string fileName, string line, IUserConfiguration user, ProcessResult result)
        {
            recipient.Attachments = new List<string>();

            foreach (FieldConfiguration field in templateConfiguration.Fields.Where(x => x.IsAttachment))
            {
                string attachName = recipientArray[field.Position];

                if (!string.IsNullOrEmpty(attachName))
                {
                    string localAttachement = GetAttachmentFile(attachName, fileName, user);

                    if (!string.IsNullOrEmpty(localAttachement))
                    {
                        recipient.Attachments.Add(localAttachement);
                    }
                    else
                    {
                        string message = $"The attachment file {attachName} doesn't exists.";
                        recipient.HasError = true;
                        recipient.ResultLine = $"{line}{templateConfiguration.FieldSeparator}{message}";
                        _logger.Error(message);
                        string errorMessage = $"{DateTime.UtcNow}:{message} proccesing line {line}";
                        result.WriteError(errorMessage);
                        result.ErrorsCount++;
                    }
                }
            }
        }

        protected void AddExtraHeaders(string resultsFileName, string headers, char separator)
        {
            if (!File.Exists(resultsFileName))
            {
                string resultHeaders = $"{headers}{separator}{Constants.HEADER_PROCESS_RESULT}{separator}{Constants.HEADER_DELIVERY_RESULT}";
                using (StreamWriter sw = new StreamWriter(resultsFileName))
                {
                    sw.WriteLine(resultHeaders);
                }
            }
        }

        protected override List<string> GetAttachments(string file, string userName)
        {
            return new List<string>();
        }

        protected override string GetBody(string file, IUserConfiguration user, ProcessResult result)
        {
            string body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\FinishProcess.es.html");

            return string.Format(body, Path.GetFileNameWithoutExtension(file), user.GetUserDateTime().DateTime, result.ProcessedCount, result.ErrorsCount);
        }
    }
}
