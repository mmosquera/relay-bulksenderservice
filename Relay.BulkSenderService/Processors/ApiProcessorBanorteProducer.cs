using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System.Collections.Generic;
using System.IO;

namespace Relay.BulkSenderService.Processors
{
    public class ApiProcessorBanorteProducer : ApiProcessorProducer
    {
        public ApiProcessorBanorteProducer(IConfiguration configuration) : base(configuration)
        {
        }

        protected override void FillRecipientAttachments(ApiRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string attachmentsFolder)
        {
            var attachmentsList = new List<string>();
            string attachName = null;

            if (recipientArray.Length >= 4)
            {
                attachName = $@"{recipientArray[0]}-{recipientArray[1]}-{recipientArray[2]}-{recipientArray[3]}.pdf";
            }

            if (!string.IsNullOrEmpty(attachName))
            {
                string localAttachement = $@"{attachmentsFolder}\{attachName}";

                if (File.Exists(localAttachement))
                {
                    attachmentsList.Add(localAttachement);
                }
                else
                {
                    recipient.HasError = true;
                    recipient.ResultLine = $"The attachment file {attachName} doesn't exists.";
                }
            }

            if (attachmentsList.Count > 0)
            {
                recipient.FillAttachments(attachmentsList);
            }

            base.FillRecipientAttachments(recipient, templateConfiguration, recipientArray, attachmentsFolder);
        }
    }
}
