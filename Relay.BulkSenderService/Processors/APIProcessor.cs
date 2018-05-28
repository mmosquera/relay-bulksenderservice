using Newtonsoft.Json;
using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
                    string message = $"{DateTime.UtcNow}:Error to authenticate user {user.Name}";                    
                    result.Type = ResulType.LOGIN;
                    result.WriteError(message);
                    return null;
                }

                string fileName = Path.GetFileName(localFileName);

                resultsFileName = GetResultsFileName(fileName, (UserApiConfiguration)user);

                _logger.Debug($"Start to read file {localFileName}");

                using (StreamReader reader = new StreamReader(localFileName))
                {
                    ITemplateConfiguration templateConfiguration = ((UserApiConfiguration)user).GetTemplateConfiguration(fileName);

                    if (templateConfiguration == null)
                    {
                        string message = $"{DateTime.UtcNow}:There is not template configuration.";                        
                        result.Type = ResulType.PROCESS;
                        result.WriteError(message);
                        return resultsFileName;
                    }

                    string line = templateConfiguration.HasHeaders ? reader.ReadLine() : null;

                    string headers = GetHeaderLine(line, templateConfiguration);

                    if (string.IsNullOrEmpty(headers))
                    {
                        string message = $"{DateTime.UtcNow}:There are not headers.";                        
                        result.Type = ResulType.PROCESS;
                        result.WriteError(message);
                        return resultsFileName;
                    }

                    AddExtraHeaders(resultsFileName, headers, templateConfiguration.FieldSeparator);

                    string[] headersArray = headers.Split(templateConfiguration.FieldSeparator);

                    List<CustomHeader> customHeaders = GetHeaderList(headersArray);

                    int maxHeaderPosition = templateConfiguration.Fields.Max(x => x.Position);

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

                        line = reader.ReadLine();

                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }

                        string[] recipientArray = GetDataLine(line, templateConfiguration);

                        ApiRecipient recipient = GetRecipient(recipients, recipientArray, templateConfiguration);

                        result.ProcessedCount++;

                        if (recipientArray.Length == headersArray.Length)
                        {
                            if (recipientArray.Length > maxHeaderPosition)
                            {
                                FillRecipientBasics(recipient, recipientArray, templateConfiguration.Fields, templateId);
                                FillRecipientCustoms(recipient, recipientArray, customHeaders, templateConfiguration.Fields);
                                FillRecipientAttachments(recipient, templateConfiguration, recipientArray, fileName, line, (UserApiConfiguration)user, result);

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
                                    string errorMessage = $"{DateTime.UtcNow}:{message} proccesing line {line}";
                                    result.WriteError(errorMessage);
                                    result.ErrorsCount++;
                                }
                                else if (string.IsNullOrEmpty(recipient.ToEmail))
                                {
                                    string message = "Has not email to send.";
                                    recipient.HasError = true;
                                    recipient.ResultLine = $"{line}{templateConfiguration.FieldSeparator}{message}";
                                    _logger.Error(message);
                                    string errorMessage = $"{DateTime.UtcNow}:{message} proccesing line {line}";
                                    result.WriteError(errorMessage);
                                    result.ErrorsCount++;
                                }
                            }
                            else
                            {
                                string message = "Wrong recipient data.";
                                recipient.HasError = true;
                                recipient.ResultLine = $"{line}{templateConfiguration.FieldSeparator}{message}";
                                _logger.Error(message);
                                string errorMessage = $"{DateTime.UtcNow}:{message} proccesing line {line}";
                                result.WriteError(errorMessage);
                                result.ErrorsCount++;
                            }
                        }
                        else
                        {
                            string message = "The fields number is different to headers number.";
                            recipient.HasError = true;
                            recipient.ResultLine = $"{line}{templateConfiguration.FieldSeparator}{message}";
                            _logger.Error(message);
                            string errorMessage = $"{DateTime.UtcNow}:{message} proccesing line {line}";
                            result.WriteError(errorMessage);
                            result.ErrorsCount++;
                        }

                        AddRecipient(recipients, recipient);

                        if (recipients.Count() == _configuration.BulkEmailCount)
                        {
                            SendRecipientsList(recipients, resultsFileName, templateConfiguration.FieldSeparator, result, user.Credentials);
                        }
                    }

                    SendRecipientsList(recipients, resultsFileName, templateConfiguration.FieldSeparator, result, user.Credentials);
                }
            }
            catch (Exception e)
            {
                // TODO check if needed return null.
                string message = $"{DateTime.UtcNow}:GENERAL ERROR PROCESS contact admin for more information.";
                result.WriteError(message);
                _logger.Error($"ERROR on process file {localFileName} -- {e}");
            }

            return resultsFileName;
        }

        private bool ValidateCredentials(CredentialsConfiguration credentials)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_configuration.BaseUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", $"token {credentials.ApiKey}");

                string accountUrl = _configuration.AccountUrl.Replace("{AccountId}", credentials.AccountId.ToString());

                try
                {
                    HttpResponseMessage response = client.GetAsync(accountUrl).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        string result = response.Content.ReadAsStringAsync().Result;
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
        }

        protected virtual void AddRecipient(List<ApiRecipient> recipients, ApiRecipient recipient)
        {
            recipients.Add(recipient);
        }

        protected virtual string[] GetDataLine(string line, ITemplateConfiguration templateConfiguration)
        {
            return line.Split(templateConfiguration.FieldSeparator);
        }

        protected virtual string GetHeaderLine(string line, ITemplateConfiguration templateConfiguration)
        {
            if (templateConfiguration != null && !templateConfiguration.HasHeaders)
            {
                return string.Join(templateConfiguration.FieldSeparator.ToString(), templateConfiguration.Fields.Select(x => x.Name));
            }

            return line;
        }

        protected string GetResultsFileName(string fileName, UserApiConfiguration user)
        {
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            ITemplateConfiguration templateConfiguration = user.GetTemplateConfiguration(fileName);

            string extension = ".sent";
            if (user.Reports == null || user.Reports.ReportsList.Count == 0)
            {
                extension = ".report";
            }

            string resultsFileName = $@"{filePathHelper.GetResultsFilesFolder()}\{fileName.Replace(".processing", extension)}";

            return resultsFileName;
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

        protected void SendEmail(string apiKey, int accountId, ApiRecipient recipient, char separator, ProcessResult result)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_configuration.BaseUrl);
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", $"token {apiKey}");

                dynamic dinObject = DictionaryToObject(recipient.Fields);

                // To test with json file.
                //string jsonString;
                //using (StreamReader reader = new StreamReader("jsonToTest.json"))
                //{
                //    jsonString = reader.ReadToEnd();
                //}
                //dynamic dinObject = JsonConvert.DeserializeObject(jsonString);

                try
                {
                    string uriTemplate = _configuration.TemplateUrl.Replace("{AccountId}", accountId.ToString()).Replace("{TemplateId}", recipient.TemplateId);

                    string postData = JsonConvert.SerializeObject(new
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

                    HttpResponseMessage response = httpClient.PostAsync(uriTemplate, new StringContent(postData, Encoding.UTF8, "application/json")).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        //_logger.Debug($"{response.StatusCode} -- Send OK to {recipient.ToEmail}");
                        string responseResult = response.Content.ReadAsStringAsync().Result;
                        var apiResult = JsonConvert.DeserializeObject<ApiResponse>(responseResult);

                        // TODO: Improvements to add results.
                        string linkResult = apiResult._links.Count >= 2 ? apiResult._links[1].href : string.Empty;
                        string sentResult = $"Send OK{separator}{apiResult.createdResourceId}{separator}{linkResult}";
                        recipient.AddSentResult(separator, sentResult);
                    }
                    else
                    {
                        // TODO: send login error.
                        //if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) 
                        dynamic jsonResult = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);

                        _logger.Info($"{response.StatusCode} -- Send fail to {recipient.ToEmail} -- {jsonResult}");

                        string message = $"{DateTime.UtcNow}:{response.StatusCode} -- Send fail to {recipient.ToEmail}";
                        result.WriteError(message);

                        result.ErrorsCount++;

                        recipient.AddSentResult(separator, $"Send Fail ({jsonResult.title})");
                    }
                }
                catch (Exception se)
                {
                    string message = $"{DateTime.UtcNow}:Send error to {recipient.ToEmail}";
                    result.WriteError(message);

                    result.ErrorsCount++;

                    _logger.Error($"SENDING ERROR to \"{recipient.ToEmail}\". -- {se}");
                }
            }
        }

        protected virtual void SendRecipientsList(List<ApiRecipient> recipients, string resultsFileName, char separator, ProcessResult result, CredentialsConfiguration credentials)
        {
            using (StreamWriter sw = new StreamWriter(resultsFileName, true))
            {
                foreach (ApiRecipient recipient in recipients)
                {
                    if (!recipient.HasError)
                    {
                        SendEmail(credentials.ApiKey, credentials.AccountId, recipient, separator, result);

                        Thread.Sleep(_configuration.DeliveryInterval);
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
                        string errorMessage = $"{DateTime.UtcNow}:{message} proccesing line {line}";
                        result.WriteError(errorMessage);
                        result.ErrorsCount++;
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
    }
}
