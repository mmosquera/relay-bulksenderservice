using Newtonsoft.Json;
using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Queues;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace Relay.BulkSenderService.Processors
{
    public class APIProcessor : Processor
    {
        public APIProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration) { }

        protected override string Process(IUserConfiguration user, string localFileName, ProcessResult result)
        {
            string resultsFileName = string.Empty;

            if (string.IsNullOrEmpty(localFileName))
            {
                return null;
            }

            try
            {
                if (!ValidateCredentials(user.Credentials))
                {
                    result.AddLoginError();
                    return null;
                }

                string fileName = Path.GetFileName(localFileName);

                resultsFileName = GetResultsFileName(fileName, (UserApiConfiguration)user);

                ITemplateConfiguration templateConfiguration = ((UserApiConfiguration)user).GetTemplateConfiguration(fileName);

                if (templateConfiguration == null)
                {
                    result.AddProcessError(_lineNumber, "There is not template configuration.");
                    return resultsFileName;
                }

                int totalLines = templateConfiguration.HasHeaders ? GetTotalLines(localFileName) - 1 : GetTotalLines(localFileName);
                result.SetTotalCount(totalLines);

                CustomProcessForFile(localFileName, user.Name, templateConfiguration);

                _logger.Debug($"Start to read file {localFileName}");

                using (StreamReader reader = new StreamReader(localFileName))
                {
                    string line = null;

                    if (templateConfiguration.HasHeaders)
                    {
                        line = reader.ReadLine();
                        _lineNumber++;
                    }

                    string headers = GetHeaderLine(line, templateConfiguration);

                    if (string.IsNullOrEmpty(headers))
                    {
                        result.AddProcessError(_lineNumber, "The file has not headers.");
                        return resultsFileName;
                    }

                    AddExtraHeaders(resultsFileName, headers, templateConfiguration.FieldSeparator);

                    string[] headersArray = headers.Split(templateConfiguration.FieldSeparator);

                    List<CustomHeader> customHeaders = GetHeaderList(headersArray);

                    string templateId = templateConfiguration != null ? templateConfiguration.TemplateId : null;

                    var recipients = new List<ApiRecipient>();

                    _logger.Debug($"Start process {fileName}");

                    while (!reader.EndOfStream)
                    {
                        if (MustStop())
                        {
                            _logger.Debug($"Stop send process file:{fileName} for user:{user.Name}");
                            // TODO: add generate retry file

                            return null;
                        }

                        GetProcessStatus(user, result);

                        line = reader.ReadLine();

                        if (string.IsNullOrEmpty(line))
                        {
                            _lineNumber++;
                            continue;
                        }

                        string[] recipientArray = GetDataLine(line, templateConfiguration);

                        ApiRecipient recipient = GetRecipient(recipients, recipientArray, templateConfiguration);
                        recipient.LineNumber = _lineNumber;

                        result.AddProcessed();

                        if (recipientArray.Length == headersArray.Length)
                        {
                            FillRecipientBasics(recipient, recipientArray, templateConfiguration.Fields, templateId);
                            FillRecipientCustoms(recipient, recipientArray, customHeaders, templateConfiguration.Fields);
                            //TODO : move to consumer
                            FillRecipientAttachments(recipient, templateConfiguration, recipientArray, fileName, line, (UserApiConfiguration)user, result);
                            HostFile(recipient, templateConfiguration, recipientArray, line, fileName, user, result);

                            if (!recipient.HasError && !string.IsNullOrEmpty(recipient.TemplateId) && !string.IsNullOrEmpty(recipient.ToEmail))
                            {
                                recipient.ResultLine = $"{line}{templateConfiguration.FieldSeparator}{Constants.PROCESS_RESULT_OK}";
                            }
                            else if (string.IsNullOrEmpty(recipient.TemplateId))
                            {
                                string message = "Has not template to send.";
                                recipient.HasError = true;
                                recipient.ResultLine = $"{line}{templateConfiguration.FieldSeparator}{message}";
                                _logger.Error(message);
                                result.AddProcessError(_lineNumber, message);
                            }
                            else if (string.IsNullOrEmpty(recipient.ToEmail))
                            {
                                string message = "Has not email to send.";
                                recipient.HasError = true;
                                recipient.ResultLine = $"{line}{templateConfiguration.FieldSeparator}{message}";
                                _logger.Error(message);
                                result.AddProcessError(_lineNumber, message);
                            }
                        }
                        else
                        {
                            string message = "The fields number is different to headers number.";
                            recipient.HasError = true;

                            string newLine = "";
                            for (int i = 0; i < headersArray.Length; i++)
                            {
                                if (recipientArray.Length > i)
                                {
                                    newLine += $"{recipientArray[i]}{templateConfiguration.FieldSeparator}";
                                }
                                else
                                {
                                    newLine += $"{templateConfiguration.FieldSeparator}";
                                }
                            }

                            recipient.ResultLine = $"{newLine}{message}";

                            _logger.Error(message);
                            result.AddProcessError(_lineNumber, message);
                        }

                        CustomRecipientValidations(recipient, recipientArray, line, templateConfiguration.FieldSeparator, result);

                        //TODO: replace for add to queue
                        AddRecipient(recipients, recipient);

                        if (recipients.Count() == _configuration.BulkEmailCount)
                        {
                            SendRecipientsList(recipients, resultsFileName, templateConfiguration.FieldSeparator, result, user.Credentials, user.DeliveryDelay);
                        }

                        _lineNumber++;
                    }

                    SendRecipientsList(recipients, resultsFileName, templateConfiguration.FieldSeparator, result, user.Credentials, user.DeliveryDelay);

                    result.Finished = true;

                    GetProcessStatus(user, result);
                }
            }
            catch (Exception e)
            {
                // TODO check if needed return null.                
                result.AddUnexpectedError(_lineNumber);
                _logger.Error($"ERROR on process file {localFileName} -- {e}");
            }

            return resultsFileName;
        }

        protected virtual void HostFile(ApiRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string line, string originalFileName, IUserConfiguration user, ProcessResult result)
        {

        }

        protected virtual void CustomRecipientValidations(ApiRecipient recipient, string[] recipientArray, string line, char fielSeparator, ProcessResult result)
        {

        }

        protected virtual void CustomProcessForFile(string fileName, string userName, ITemplateConfiguration templateConfiguration)
        {

        }

        public bool ValidateCredentials(CredentialsConfiguration credentials)
        {
            var restClient = new RestClient(_configuration.BaseUrl);

            string resource = _configuration.AccountUrl.Replace("{AccountId}", credentials.AccountId.ToString());
            var request = new RestRequest(resource, Method.GET);

            string value = $"token {credentials.ApiKey}";
            request.AddHeader("Authorization", value);

            try
            {
                IRestResponse response = restClient.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    string result = response.Content;
                    _logger.Info($"Validate credentials fail:{result}");
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Validate credentials error -- {e}");
                return false;
            }
        }

        protected virtual void AddRecipient(List<ApiRecipient> recipients, ApiRecipient recipient)
        {
            recipients.Add(recipient);
        }

        protected virtual string[] GetDataLine(string line, ITemplateConfiguration templateConfiguration)
        {
            return line.Split(templateConfiguration.FieldSeparator);
        }

        private void AddExtraHeaders(string resultsFileName, string headers, char separator)
        {
            if (!File.Exists(resultsFileName))
            {
                string resultHeaders = $"{headers}{separator}{Constants.HEADER_PROCESS_RESULT}{separator}{Constants.HEADER_DELIVERY_RESULT}{separator}{Constants.HEADER_MESSAGE_ID}{separator}{Constants.HEADER_DELIVERY_LINK}";
                using (StreamWriter sw = new StreamWriter(resultsFileName))
                {
                    sw.WriteLine(resultHeaders);
                }
            }
        }

        protected void SendEmailWithRetries(string apiKey, int accountId, ApiRecipient recipient, char separator, ProcessResult result)
        {
            int count = 0;

            while (count < _configuration.DeliveryRetryCount && !SendEmail(apiKey, accountId, recipient, separator, result))
            {
                count++;

                if (count == _configuration.DeliveryRetryCount)
                {
                    result.AddUnexpectedError(recipient.LineNumber);
                }
                else
                {
                    Thread.Sleep(_configuration.DeliveryRetryInterval);
                }
            }
        }

        protected bool SendEmail(string apiKey, int accountId, ApiRecipient recipient, char separator, ProcessResult result)
        {
            var restClient = new RestClient(_configuration.BaseUrl);

            string resource = _configuration.TemplateUrl.Replace("{AccountId}", accountId.ToString()).Replace("{TemplateId}", recipient.TemplateId);
            var request = new RestRequest(resource, Method.POST);

            string value = $"token {apiKey}";
            request.AddHeader("Authorization", value);

            dynamic dinObject = DictionaryToObject(recipient.Fields);

            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(new
            {
                from_name = recipient.FromName,
                from_email = recipient.FromEmail,
                recipients = new[] { new {
                                email = recipient.ToEmail,
                                name = recipient.ToName,
                                type = "to" }
                            },
                reply_to = !string.IsNullOrEmpty(recipient.ReplyToEmail) ? new
                {
                    email = recipient.ReplyToEmail,
                    name = recipient.ReplyToName
                } : null,
                model = dinObject,
                attachments = recipient.Attachments
            });

            try
            {
                IRestResponse response = restClient.Execute(request);

                if (response.IsSuccessful)
                {
                    var apiResult = JsonConvert.DeserializeObject<ApiResponse>(response.Content);

                    // TODO: Improvements to add results.
                    string linkResult = apiResult._links.Count >= 2 ? apiResult._links[1].href : string.Empty;

                    string sentResult = $"Send OK{separator}{apiResult.createdResourceId}{separator}{linkResult}";

                    recipient.AddSentResult(separator, sentResult);
                }
                else
                {
                    dynamic jsonResult = JsonConvert.DeserializeObject(response.Content);

                    _logger.Info($"{response.StatusCode} -- Send fail to {recipient.ToEmail} -- {jsonResult}");

                    result.AddDeliveryError(recipient.LineNumber, response.StatusCode.ToString());

                    recipient.AddSentResult(separator, $"Send Fail ({jsonResult.title})");
                }

                /************TO TEST PROCESS WITHOUT SEND***********************/
                //string resourceid = "fakeresourceid";
                //string linkResult = "deliverylink";
                //string sentResult = $"Send OK{separator}{resourceid}{separator}{linkResult}";
                //recipient.AddSentResult(separator, sentResult);
                //Thread.Sleep(200);
                /***************************************************************/

                return true;
            }
            catch (Exception se)
            {
                _logger.Error($"SENDING ERROR to: {recipient.ToEmail}. -- {se}");

                return false;
            }
        }

        protected virtual void SendRecipientsList(List<ApiRecipient> recipients, string resultsFileName, char separator, ProcessResult result, CredentialsConfiguration credentials, int deliveryDelay)
        {
            using (StreamWriter sw = new StreamWriter(resultsFileName, true))
            {
                foreach (ApiRecipient recipient in recipients)
                {
                    if (!recipient.HasError)
                    {
                        SendEmailWithRetries(credentials.ApiKey, credentials.AccountId, recipient, separator, result);

                        Thread.Sleep(deliveryDelay);
                    }
                    sw.WriteLine(recipient.ResultLine);
                    sw.Flush();
                }
            }

            recipients.Clear();
        }

        private dynamic DictionaryToObject(Dictionary<string, object> dictionary)
        {
            IDictionary<string, object> expandedObject = new ExpandoObject() as IDictionary<string, object>;

            foreach (KeyValuePair<string, object> key in dictionary)
            {
                expandedObject.Add(key);
            }

            return expandedObject;
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

        protected virtual void FillRecipientAttachments(ApiRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string fileName, string line, UserApiConfiguration user, ProcessResult result)
        {
            var attachmentsList = new List<string>();

            foreach (FieldConfiguration field in templateConfiguration.Fields.Where(x => x.IsAttachment))
            {
                string attachName = recipientArray[field.Position];

                if (!string.IsNullOrEmpty(attachName))
                {
                    string localAttachement = GetAttachmentFile(attachName, fileName, user);

                    if (!string.IsNullOrEmpty(localAttachement))
                    {
                        attachmentsList.Add(localAttachement);
                    }
                    else
                    {
                        string message = $"The attachment file {attachName} doesn't exists.";
                        recipient.HasError = true;
                        recipient.ResultLine = $"{line}{templateConfiguration.FieldSeparator}{message}";
                        _logger.Error(message);
                        result.AddProcessError(_lineNumber, message);
                    }
                }
            }

            if (attachmentsList.Count > 0)
            {
                recipient.FillAttachments(attachmentsList);
            }
        }

        protected virtual ApiRecipient GetRecipient(List<ApiRecipient> recipients, string[] recipientArray, ITemplateConfiguration templateConfiguration)
        {
            return new ApiRecipient();
        }

        protected override string GetBody(string file, IUserConfiguration user, ProcessResult result)
        {
            string body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\FinishProcess.es.html");

            return body.Replace("{{filename}}", Path.GetFileNameWithoutExtension(file))
                .Replace("{{time}}", user.GetUserDateTime().DateTime.ToString())
                .Replace("{{processed}}", result.GetProcessedCount().ToString())
                .Replace("{{errors}}", result.GetErrorsCount().ToString());
        }

        protected override List<string> GetAttachments(string file, string usarName)
        {
            return new List<string>();
        }

        protected override IQueueProducer GetProducer()
        {
            IQueueProducer producer = new ApiProcessorProducer(_configuration);
            producer.ErrorEvent += Processor_ErrorEvent;

            return producer;
        }

        protected override List<IQueueConsumer> GetConsumers(int count)
        {
            var consumers = new List<IQueueConsumer>();

            for (int i = 0; i < count; i++)
            {
                IQueueConsumer consumer = new ApiProcessorConsumer(_configuration, _logger);
                consumer.ErrorEvent += Processor_ErrorEvent;
                consumer.ResultEvent += Processor_ResultEvent;
                consumers.Add(consumer);
            }

            return consumers;
        }
    }
}
