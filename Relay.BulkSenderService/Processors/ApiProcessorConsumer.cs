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
        public event EventHandler<QueueResultEventArgs> ResultEvent;
        public event EventHandler<QueueErrorEventArgs> ErrorEvent;

        public void ProcessMessages(IConfiguration configuration, IUserConfiguration userConfiguration, IBulkQueue queue, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            //var waitTime = TimeSpan.FromSeconds(2);
            //int retries = 0;              

            IBulkQueueMessage bulkQueueMessage = null;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    //cancellationToken.ThrowIfCancellationRequested();
                    break;
                }

                //if (retries == 3)
                //{
                //    break;
                //}

                bulkQueueMessage = queue.ReceiveMessage();

                if (bulkQueueMessage != null)
                {
                    SendEmailWithRetries(configuration, userConfiguration, (ApiRecipient)bulkQueueMessage);

                    bool result = true;
                    string text;

                    if (result)
                    {
                        text = "OK";
                        var resultEventArgs = new QueueResultEventArgs()
                        {
                            LineNumber = bulkQueueMessage.LineNumber,
                            Message = text
                        };
                        ResultEvent?.Invoke(this, resultEventArgs);
                    }
                    else
                    {
                        text = "ERROR";
                        var errorEventArgs = new QueueErrorEventArgs()
                        {
                            LineNumber = bulkQueueMessage.LineNumber,
                            Type = ErrorType.DELIVERY,
                            Date = DateTime.UtcNow,
                            Message = text
                        };
                        ErrorEvent?.Invoke(this, errorEventArgs);
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        protected bool SendEmailWithRetries(IConfiguration configuration, IUserConfiguration userConfiguration, ApiRecipient apiRecipient)
        {
            int count = 0;

            while (count < configuration.DeliveryRetryCount)
            {
                count++;

                try
                {
                    return SendEmail(configuration.BaseUrl, configuration.TemplateUrl, userConfiguration.Credentials.ApiKey, userConfiguration.Credentials.AccountId, apiRecipient);
                }
                catch (Exception e)
                {
                    if (count == configuration.DeliveryRetryCount)
                    {
                        apiRecipient.Message = "Unexpected error. Contact support for more information.";
                        //result.AddUnexpectedError(recipient.LineNumber);
                        //error
                    }
                    else
                    {
                        Thread.Sleep(configuration.DeliveryRetryInterval);
                    }
                }
            }

            return false;
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

                    string sentResult = $"Send OK|{apiResult.createdResourceId}|{linkResult}";
                    apiRecipient.Message = sentResult;
                    //recipient.AddSentResult(separator, sentResult);
                    return true;
                }
                else
                {
                    dynamic jsonResult = JsonConvert.DeserializeObject(response.Content);

                    //_logger.Info($"{response.StatusCode} -- Send fail to {recipient.ToEmail} -- {jsonResult}");
                    //result.AddDeliveryError(recipient.LineNumber, response.StatusCode.ToString());
                    //recipient.AddSentResult(separator, $"Send Fail ({jsonResult.title})");
                    apiRecipient.Message = $"Send Fail ({jsonResult.title})";

                    return false;
                }

                /************TO TEST PROCESS WITHOUT SEND***********************/
                //string resourceid = "fakeresourceid";
                //string linrkResult = "deliverylink";
                //string sentResult = $"Send OK{separator}{resourceid}{separator}{linkResult}";
                //recipient.AddSentResult(separator, sentResult);
                //Thread.Sleep(200);
                /***************************************************************/
            }
            catch (Exception se)
            {
                //_logger.Error($"SENDING ERROR to: {recipient.ToEmail}. -- {se}");

                throw se;
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
