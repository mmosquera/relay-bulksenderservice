using Newtonsoft.Json;
using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Queues;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;

namespace Relay.BulkSenderService.Processors
{
    public class ApiProcessorConsumer : IQueueConsumer
    {
        private readonly IConfiguration _configuration;
        private readonly ILog _logger;
        public event EventHandler<QueueResultEventArgs> ResultEvent;
        public event EventHandler<QueueErrorEventArgs> ErrorEvent;

        public ApiProcessorConsumer(IConfiguration configuration, ILog logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void ProcessMessages(IUserConfiguration userConfiguration, IBulkQueue queue, CancellationToken cancellationToken)
        {
            IBulkQueueMessage bulkQueueMessage = null;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                bulkQueueMessage = queue.ReceiveMessage();

                if (bulkQueueMessage != null)
                {
                    SendEmailWithRetries(_configuration, userConfiguration, (ApiRecipient)bulkQueueMessage);
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        protected void SendEmailWithRetries(IConfiguration configuration, IUserConfiguration userConfiguration, ApiRecipient apiRecipient)
        {
            int count = 0;

            while (count < configuration.DeliveryRetryCount && !SendEmail(configuration.BaseUrl, configuration.TemplateUrl, userConfiguration.Credentials.ApiKey, userConfiguration.Credentials.AccountId, apiRecipient))
            {
                count++;

                if (count == configuration.DeliveryRetryCount)
                {
                    var errorEventArgs = new QueueErrorEventArgs()
                    {
                        LineNumber = apiRecipient.LineNumber,
                        Type = ErrorType.DELIVERY,
                        Date = DateTime.UtcNow,
                        Message = "Unexpected error.Contact support for more information."
                    };
                    ErrorEvent?.Invoke(this, errorEventArgs);
                }
                else
                {
                    Thread.Sleep(configuration.DeliveryRetryInterval);
                }
            }
        }

        /// <summary>
        /// To test emails without API
        /// </summary>        
        protected bool SendEmailTest(string baseUrl, string templateUrl, string apiKey, int accountId, ApiRecipient apiRecipient)
        {
            dynamic dinObject = DictionaryToObject(apiRecipient.Fields);

            object body = new
            {
                from_name = apiRecipient.FromName,
                from_email = apiRecipient.FromEmail,
                recipients = new[] { new
                {
                    email = apiRecipient.ToEmail,
                    name = apiRecipient.ToName,
                    type = "to"
                }},
                reply_to = !string.IsNullOrEmpty(apiRecipient.ReplyToEmail) ? new
                {
                    email = apiRecipient.ReplyToEmail,
                    name = apiRecipient.ReplyToName
                } : null,
                model = dinObject,
                attachments = apiRecipient.Attachments
            };

            string resourceId = "20191022-1932-0811-afb5-3e64c86f3245";
            string linkResult = "/accounts/27/deliveries/20191022-1932-055f-aa29-c288e933a58a";

            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*");
            bool mailValid = regex.IsMatch(apiRecipient.ToEmail);

            Random r = new Random();
            int ms = r.Next(10, 90);
            Thread.Sleep(ms);

            if (mailValid)
            {
                var resultEventArgs = new QueueResultEventArgs()
                {
                    LineNumber = apiRecipient.LineNumber,
                    Message = "Send OK",
                    ResourceId = resourceId,
                    DeliveryLink = linkResult,
                    EnqueueTime = apiRecipient.EnqueueTime,
                    DequeueTime = apiRecipient.DequeueTime,
                    DeliveryTime = DateTime.UtcNow
                };
                ResultEvent?.Invoke(this, resultEventArgs);
            }
            else
            {
                string content = "{\"title\":\"Validation error\",\"message\":\"Validationerror\"}";

                dynamic jsonResult = JsonConvert.DeserializeObject(content);

                //_logger.Info($"{response.StatusCode} -- Send fail to {recipient.ToEmail} -- {jsonResult}");
                //result.AddDeliveryError(recipient.LineNumber, response.StatusCode.ToString());                    

                var errorEventArgs = new QueueErrorEventArgs()
                {
                    LineNumber = apiRecipient.LineNumber,
                    Type = ErrorType.DELIVERY,
                    Date = DateTime.UtcNow,
                    Message = jsonResult.title,
                    Description = jsonResult.ToString(),
                    EnqueueTime = apiRecipient.EnqueueTime,
                    DequeueTime = apiRecipient.DequeueTime,
                    DeliveryTime = DateTime.UtcNow
                };
                ErrorEvent?.Invoke(this, errorEventArgs);
            }

            return true;
        }

        protected bool SendEmail(string baseUrl, string templateUrl, string apiKey, int accountId, ApiRecipient apiRecipient)
        {
            var restClient = new RestClient(baseUrl);

            string resource = templateUrl.Replace("{AccountId}", accountId.ToString()).Replace("{TemplateId}", apiRecipient.TemplateId);
            var request = new RestRequest(resource, Method.POST);

            string value = $"token {apiKey}";
            request.AddHeader("Authorization", value);

            dynamic dinObject = DictionaryToObject(apiRecipient.Fields);

            object body = new
            {
                from_name = apiRecipient.FromName,
                from_email = apiRecipient.FromEmail,
                recipients = new[] { new
                {
                    email = apiRecipient.ToEmail,
                    name = apiRecipient.ToName,
                    type = "to"
                }},
                reply_to = !string.IsNullOrEmpty(apiRecipient.ReplyToEmail) ? new
                {
                    email = apiRecipient.ReplyToEmail,
                    name = apiRecipient.ReplyToName
                } : null,
                model = dinObject,
                attachments = apiRecipient.Attachments
            };

            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(body);

            try
            {
                IRestResponse response = restClient.Execute(request);

                if (response.IsSuccessful)
                {
                    var apiResult = JsonConvert.DeserializeObject<ApiResponse>(response.Content);

                    // TODO: Improvements to add results.
                    string linkResult = apiResult._links.Count >= 2 ? apiResult._links[1].href : string.Empty;

                    var resultEventArgs = new QueueResultEventArgs()
                    {
                        LineNumber = apiRecipient.LineNumber,
                        Message = "Send OK",
                        ResourceId = apiResult.createdResourceId.ToString(),
                        DeliveryLink = linkResult,
                        EnqueueTime = apiRecipient.EnqueueTime,
                        DequeueTime = apiRecipient.DequeueTime,
                        DeliveryTime = DateTime.UtcNow
                    };
                    ResultEvent?.Invoke(this, resultEventArgs);
                }
                else
                {
                    dynamic jsonResult = JsonConvert.DeserializeObject(response.Content);

                    //_logger.Info($"{response.StatusCode} -- Send fail to {recipient.ToEmail} -- {jsonResult}");
                    //result.AddDeliveryError(recipient.LineNumber, response.StatusCode.ToString());                    

                    var errorEventArgs = new QueueErrorEventArgs()
                    {
                        LineNumber = apiRecipient.LineNumber,
                        Type = ErrorType.DELIVERY,
                        Date = DateTime.UtcNow,
                        Message = jsonResult.title,
                        Description = jsonResult.ToString(),
                        EnqueueTime = apiRecipient.EnqueueTime,
                        DequeueTime = apiRecipient.DequeueTime,
                        DeliveryTime = DateTime.UtcNow
                    };
                    ErrorEvent?.Invoke(this, errorEventArgs);
                }

                return true;

                /************TO TEST PROCESS WITHOUT SEND***********************/
                //string resourceid = "fakeresourceid";
                //string linrkResult = "deliverylink";
                //string sentResult = $"Send OK{separator}{resourceid}{separator}{linkResult}";
                //recipient.AddSentResult(separator, sentResult);
                //Thread.Sleep(200);
                /***************************************************************/
            }
            catch (Exception e)
            {
                _logger.Error($"SENDING ERROR to: {apiRecipient.ToEmail}. -- {e}");

                return false;
            }
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
    }
}
