using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Processors
{
    public class APIProcessorBanorte : APIProcessor
    {
        public APIProcessorBanorte(ILog logger, IConfiguration configuration) : base(logger, configuration) { }

        protected override void FillRecipientAttachments(ApiRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string fileName, string line, UserApiConfiguration user, ProcessResult result)
        {
            var attachmentsList = new List<string>();
            string attachName = null;
            if (recipientArray.Length >= 4)
            {
                attachName = $@"{recipientArray[0]}-{recipientArray[1]}-{recipientArray[2]}-{recipientArray[3]}.pdf";
            }

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

            if (attachmentsList.Count > 0)
            {
                recipient.FillAttachments(attachmentsList);
            }

            base.FillRecipientAttachments(recipient, templateConfiguration, recipientArray, fileName, line, user, result);
        }
    }
}
