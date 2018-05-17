using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Relay.BulkSenderService.Processors
{
    public class APIProcessorKeyIterator : APIProcessor
    {
        public APIProcessorKeyIterator(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {

        }

        protected override List<CustomHeader> GetHeaderList(string[] headersArray)
        {
            var customHeaders = new List<CustomHeader>();

            for (int i = 0; i < headersArray.Length; i++)
            {
                if (!customHeaders.Exists(x => x.HeaderName.Equals(headersArray[i])))
                {
                    customHeaders.Add(new CustomHeader()
                    {
                        HeaderName = headersArray[i],
                        Position = i
                    });
                }
            }

            return customHeaders;
        }

        protected override void AddRecipient(List<ApiRecipient> recipients, ApiRecipient recipient)
        {
            if (!recipients.Exists(x => x.Key.Equals(recipient.Key, StringComparison.InvariantCultureIgnoreCase)))
            {
                recipients.Add(recipient);
            }
        }

        protected override void FillRecipientBasics(ApiRecipient recipient, string[] data, List<FieldConfiguration> fields, string templateId = null)
        {
            if (string.IsNullOrEmpty(recipient.Key))
            {
                base.FillRecipientBasics(recipient, data, fields, templateId);

                int position = fields.FirstOrDefault(x => x.IsKey).Position;

                string key = data[position];

                recipient.Key = key;
            }
        }

        protected override void FillRecipientCustoms(ApiRecipient recipient, string[] data, List<CustomHeader> headerList, List<FieldConfiguration> fields)
        {
            List<FieldConfiguration> listFields = fields.Where(x => x.IsForList).ToList();
            List<CustomHeader> customFields = headerList.Where(x => !fields.Any(y => y.IsBasic && y.Position == x.Position) && !listFields.Any(z => z.Position == x.Position)).ToList();

            foreach (CustomHeader customHeader in customFields)
            {
                if (!recipient.Fields.ContainsKey(customHeader.HeaderName) && !string.IsNullOrEmpty(data[customHeader.Position]))
                {
                    recipient.Fields.Add(customHeader.HeaderName, data[customHeader.Position]);
                }
            }

            List<Dictionary<string, object>> list;

            if (!recipient.Fields.ContainsKey("list"))
            {
                list = new List<Dictionary<string, object>>();
                recipient.Fields.Add("list", list);
            }
            else
            {
                list = recipient.Fields["list"] as List<Dictionary<string, object>>;
            }

            var item = new Dictionary<string, object>();

            foreach (FieldConfiguration fieldConfiguration in listFields)
            {
                if (!item.ContainsKey(fieldConfiguration.Name) && !string.IsNullOrEmpty(data[fieldConfiguration.Position]))
                {
                    item.Add(fieldConfiguration.Name, data[fieldConfiguration.Position]);
                }
            }

            list.Add(item);
        }

        protected override ApiRecipient GetRecipient(List<ApiRecipient> recipients, string[] recipientArray, ITemplateConfiguration templateConfiguration)
        {
            int position = templateConfiguration.Fields.FirstOrDefault(x => x.IsKey).Position;

            string key = recipientArray[position];

            ApiRecipient recipient = recipients.Where(x => x.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (recipient == null)
            {
                recipient = new ApiRecipient();
            }

            return recipient;
        }

        protected override void SendRecipientsList(List<ApiRecipient> recipients, string resultsFileName, char separator, ProcessResult result, CredentialsConfiguration credentials)
        {
            ApiRecipient recipient = null;
            int count = recipients.Count();

            if (count == _configuration.BulkEmailCount)
            {
                count -= 1;
                recipient = recipients[count];
            }

            using (StreamWriter sw = new StreamWriter(resultsFileName, true))
            {
                for (int i = 0; i < count; i++)
                {
                    if (!recipients[i].HasError)
                    {
                        SendEmail(credentials.ApiKey, credentials.AccountId, recipients[i], separator, result);

                        Thread.Sleep(_configuration.DeliveryInterval);
                    }
                    sw.WriteLine(recipients[i].ResultLine);
                    sw.Flush();
                }
            }

            recipients.Clear();

            if (recipient != null)
            {
                recipients.Add(recipient);
            }
        }
    }
}
