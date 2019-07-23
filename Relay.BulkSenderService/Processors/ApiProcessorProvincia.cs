using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Processors
{
    public class ApiProcessorProvincia : APIProcessor
    {
        private readonly Dictionary<string, string> _hostedFiles;

        public ApiProcessorProvincia(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
            _hostedFiles = new Dictionary<string, string>();
        }

        protected override void FillRecipientAttachments(ApiRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string fileName, string line, UserApiConfiguration user, ProcessResult result)
        {
            var attachmentsList = new List<string>();

            foreach (FieldConfiguration field in templateConfiguration.Fields.Where(x => x.IsAttachment))
            {
                string[] attachments = recipientArray[field.Position].Split(';');

                foreach (string attachName in attachments)
                {
                    if (string.IsNullOrEmpty(attachName))
                    {
                        continue;
                    }

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

        protected override void HostFile(ApiRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string line, string originalFileName, IUserConfiguration user, ProcessResult result)
        {
            string hostedFile = recipientArray[5];

            if (string.IsNullOrEmpty(hostedFile))
            {
                hostedFile = "pieza.jpg";
            }

            string publicPath = string.Empty;

            if (_hostedFiles.ContainsKey(hostedFile))
            {
                publicPath = _hostedFiles[hostedFile];
            }
            else
            {
                var filePathHelper = new FilePathHelper(_configuration, user.Name);
                string imageFilePath = $@"{filePathHelper.GetAttachmentsFilesFolder()}\{Path.GetFileNameWithoutExtension(originalFileName)}\{hostedFile}";

                if (File.Exists(imageFilePath))
                {
                    string hostedFileName = $"{Path.GetFileNameWithoutExtension(hostedFile)}_{DateTime.Now.Ticks}{Path.GetExtension(hostedFile)}";

                    //TODO: aca va el nombre del archivo nuevo
                    string privatePath = $@"{_configuration.UserFiles}\{hostedFileName}";

                    publicPath = $"http://files.bancoprovinciamail.com.ar/relay/{hostedFileName}";

                    // Copy jpg file to be hosted.
                    File.Copy(imageFilePath, privatePath);

                    _hostedFiles.Add(hostedFile, publicPath);
                }
                else
                {
                    string message = $"The file to host {imageFilePath} doesn't exists.";
                    recipient.HasError = true;
                    recipient.ResultLine = $"{line}{templateConfiguration.FieldSeparator}{message}";
                    _logger.Error(message);
                    result.AddProcessError(_lineNumber, message);
                }
            }

            recipient.Fields.Add("hostedImage", publicPath);
        }
    }
}
