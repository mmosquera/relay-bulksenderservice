using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Queues;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Relay.BulkSenderService.Processors
{
    public class ApiProcessorProducer : IQueueProducer
    {
        private readonly IConfiguration _configuration;
        public event EventHandler<QueueErrorEventArgs> ErrorEvent;

        public ApiProcessorProducer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void GetMessages(IUserConfiguration userConfiguration, IBulkQueue queue, string localFileName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(localFileName))
            {
                return;
            }

            string fileName = Path.GetFileName(localFileName);

            ITemplateConfiguration templateConfiguration = ((UserApiConfiguration)userConfiguration).GetTemplateConfiguration(fileName);

            if (templateConfiguration == null)
            {
                throw new Exception("There is not template configuration.");
            }

            int lineNumber = 0;

            using (StreamReader reader = new StreamReader(localFileName))
            {
                string line = null;

                if (templateConfiguration.HasHeaders)
                {
                    line = reader.ReadLine();
                    lineNumber++;
                }

                string headers = GetHeaderLine(line, templateConfiguration);

                if (string.IsNullOrEmpty(headers))
                {
                    throw new Exception("The file has not headers.");
                }

                string[] headersArray = headers.Split(templateConfiguration.FieldSeparator);

                List<CustomHeader> customHeaders = GetHeaderList(headersArray);

                var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);
                string attachmentsFolder = filePathHelper.GetAttachmentsFilesFolder(Path.GetFileNameWithoutExtension(localFileName));

                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        lineNumber++;
                        continue;
                    }

                    string[] recipientArray = GetDataLine(line, templateConfiguration);

                    ApiRecipient recipient = GetRecipient(recipientArray, templateConfiguration);
                    recipient.LineNumber = lineNumber;

                    if (recipientArray.Length == headersArray.Length)
                    {
                        FillRecipientBasics(recipient, recipientArray, templateConfiguration.Fields, templateConfiguration.TemplateId);
                        FillRecipientCustoms(recipient, recipientArray, customHeaders, templateConfiguration.Fields);
                        FillRecipientAttachments(recipient, templateConfiguration, recipientArray, attachmentsFolder);

                        //TODO: meterlo solo en el processor del provincia ver como si pasar ese parametro al productor o algo asi
                        //HostFile(recipient, templateConfiguration, recipientArray, line, fileName, user, result);                                                
                        if (string.IsNullOrEmpty(recipient.TemplateId))
                        {
                            recipient.HasError = true;
                            recipient.ResultLine = "Has not template to send.";
                        }
                        else if (string.IsNullOrEmpty(recipient.ToEmail))
                        {
                            recipient.HasError = true;
                            recipient.ResultLine = "Has not email to send.";
                        }
                    }
                    else
                    {
                        recipient.HasError = true;
                        recipient.ResultLine = "The fields number is different to headers number.";
                    }

                    CustomRecipientValidations(recipient, recipientArray, line, templateConfiguration.FieldSeparator);

                    if (!recipient.HasError)
                    {
                        EnqueueRecipient(recipient, queue);
                    }
                    else
                    {
                        var args = new QueueErrorEventArgs()
                        {
                            LineNumber = recipient.LineNumber,
                            Message = recipient.ResultLine,
                            Date = DateTime.UtcNow,
                            Type = ErrorType.PROCESS
                        };

                        ErrorEvent?.Invoke(this, args);
                    }

                    lineNumber++;
                }

                //TODO: mejorar esto que se usa solo para key iterator.
                ForceEnqueue(queue);
            }
        }

        protected virtual void ForceEnqueue(IBulkQueue queue)
        {

        }

        protected virtual string GetHeaderLine(string line, ITemplateConfiguration templateConfiguration)
        {
            if (templateConfiguration != null && !templateConfiguration.HasHeaders)
            {
                return string.Join(templateConfiguration.FieldSeparator.ToString(), templateConfiguration.Fields.Select(x => x.Name));
            }

            return line;
        }

        protected virtual List<CustomHeader> GetHeaderList(string[] headersArray)
        {
            var headerList = new List<CustomHeader>();

            for (int i = 0; i < headersArray.Length; i++)
            {
                if (headerList.Exists(h => h.Position == i))
                {
                    continue;
                }

                var customHeader = new CustomHeader()
                {
                    Position = i,
                    HeaderName = headersArray[i]
                };
                headerList.Add(customHeader);
            }

            return headerList;
        }

        protected virtual string[] GetDataLine(string line, ITemplateConfiguration templateConfiguration)
        {
            return line.Split(templateConfiguration.FieldSeparator);
        }

        protected virtual ApiRecipient GetRecipient(string[] recipientArray, ITemplateConfiguration templateConfiguration)
        {
            return new ApiRecipient();
        }

        protected virtual void FillRecipientBasics(ApiRecipient recipient, string[] data, List<FieldConfiguration> fields, string templateId = null)
        {
            var fromEmailField = fields.Find(f => f.Name.Equals("fromEmail", StringComparison.OrdinalIgnoreCase));
            if (fromEmailField != null)
            {
                recipient.FromEmail = data[fromEmailField.Position];
            }

            var fromNameField = fields.Find(f => f.Name.Equals("fromName", StringComparison.OrdinalIgnoreCase));
            if (fromNameField != null)
            {
                recipient.FromName = data[fromNameField.Position];
            }

            var emailField = fields.Find(f => f.Name.Equals("email", StringComparison.OrdinalIgnoreCase));
            if (emailField != null)
            {
                recipient.ToEmail = data[emailField.Position];
            }

            var nameField = fields.Find(f => f.Name.Equals("name", StringComparison.OrdinalIgnoreCase));
            if (nameField != null)
            {
                recipient.ToName = data[nameField.Position];
            }

            var replyTo = fields.Find(f => f.Name.Equals("replyTo", StringComparison.OrdinalIgnoreCase));
            if (replyTo != null)
            {
                recipient.ReplyToEmail = data[replyTo.Position];
            }

            var replyToName = fields.Find(f => f.Name.Equals("replyToName", StringComparison.OrdinalIgnoreCase));
            if (replyToName != null)
            {
                recipient.ReplyToName = data[replyToName.Position];
            }

            var templateField = fields.Find(f => f.Name.Equals("templateid", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(templateId) && templateField != null)
            {
                recipient.TemplateId = data[templateField.Position];
            }
            else
            {
                recipient.TemplateId = templateId;
            }
        }

        protected virtual void FillRecipientCustoms(ApiRecipient recipient, string[] data, List<CustomHeader> headerList, List<FieldConfiguration> fields)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (fields.Exists(f => f.Position == i && (f.IsBasic || f.IsAttachment)) || string.IsNullOrEmpty(data[i]))
                {
                    continue;
                }

                CustomHeader customHeader = headerList.First(h => h.Position == i);

                if (customHeader != null)
                {
                    recipient.Fields.Add(customHeader.HeaderName, data[i]);
                }
            }
        }

        protected virtual void FillRecipientAttachments(ApiRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string attachmentsFolder)
        {
            var attachmentsList = new List<string>();

            foreach (FieldConfiguration field in templateConfiguration.Fields.Where(x => x.IsAttachment))
            {
                string attachName = recipientArray[field.Position];

                if (!string.IsNullOrEmpty(attachName))
                {
                    string localAttachement = $@"{attachmentsFolder}\{attachName}";


                    if (!string.IsNullOrEmpty(localAttachement))
                    {
                        attachmentsList.Add(localAttachement);
                    }
                    else
                    {
                        recipient.HasError = true;
                        recipient.ResultLine = $"The attachment file {attachName} doesn't exists.";
                    }
                }
            }

            //TODO: esto pasarlo al result??
            if (attachmentsList.Count > 0)
            {
                recipient.FillAttachments(attachmentsList);
            }
        }

        protected virtual void EnqueueRecipient(ApiRecipient recipient, IBulkQueue queue)
        {
            queue.SendMessage(recipient);
        }

        protected virtual void CustomRecipientValidations(ApiRecipient recipient, string[] recipientArray, string line, char fielSeparator)
        {

        }
    }
}
