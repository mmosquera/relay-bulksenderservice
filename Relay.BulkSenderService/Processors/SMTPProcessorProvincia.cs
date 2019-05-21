﻿using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Processors
{
    public class SMTPProcessorProvincia : SMTPProcessor
    {
        private readonly Dictionary<string, string> _hostedFiles;

        public SMTPProcessorProvincia(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
            _hostedFiles = new Dictionary<string, string>();
        }

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

                        HostFile(recipient, templateConfiguration, recipientArray, line, fileName, user, result);

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

        private string HostFile(SMTPRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string line, string originalFileName, IUserConfiguration user, ProcessResult result)
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

                    recipient.Body = $"<html><body><img src=\"{publicPath}\" /></body></html>";
                }
                else
                {
                    string message = $"The file to host {imageFilePath} doesn't exists.";
                    recipient.HasError = true;
                    recipient.ResultLine = $"{line}{templateConfiguration.FieldSeparator}{message}";
                    _logger.Error(message);
                    string errorMessage = $"{DateTime.UtcNow}:{message} proccesing line {line}";
                    result.WriteError(errorMessage);
                    result.ErrorsCount++;
                }
            }

            return publicPath;
        }

        protected override void FillRecipientAttachments(SMTPRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string fileName, string line, IUserConfiguration user, ProcessResult result)
        {
            recipient.Attachments = new List<string>();

            foreach (FieldConfiguration field in templateConfiguration.Fields.Where(x => x.IsAttachment))
            {
                string[] attachments = recipientArray[field.Position].Split(';');

                foreach (string attachName in attachments)
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
    }
}