﻿using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration.Alerts;
using Relay.BulkSenderService.Processors;
using Relay.BulkSenderService.Processors.PreProcess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Configuration
{
    public class UserSMTPConfiguration : IUserConfiguration
    {
        public int FtpInterval { get; set; }
        public bool HasDeleteFtp { get; set; }
        public string Name { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
        public string TemplateFilePath { get; set; }
        public int UserGMT { get; set; }
        public bool HasHeaders { get; set; }
        public string AttachmentsFolder { get; set; }
        public ErrorConfiguration Errors { get; set; }
        public IResultConfiguration Results { get; set; }
        public AdminEmailConfiguration AdminEmail { get; set; }
        public List<string> DownloadFolders { get; set; }
        public List<string> FileExtensions { get; set; }
        public AckConfiguration Ack { get; set; }
        public CredentialsConfiguration Credentials { get; set; }
        public IFtpConfiguration Ftp { get; set; }
        public ReportConfiguration Reports { get; set; }
        public AlertConfiguration Alerts { get; set; }
        public List<ITemplateConfiguration> Templates { get; set; }
        public IPreProcessorConfiguration PreProcessor { get; set; }
        public int MaxParallelProcessors { get; set; }

        public Processor GetProcessor(ILog logger, IConfiguration configuration, string fileName)
        {
            return GetTemplateConfiguration(fileName)?.GetProcessor(logger, configuration);
        }

        public DateTimeOffset GetUserDateTime()
        {
            var timeSpan = new TimeSpan(UserGMT, 0, 0);

            return new DateTimeOffset(DateTimeOffset.UtcNow.Add(timeSpan).DateTime, timeSpan);
        }

        public ITemplateConfiguration GetTemplateConfiguration(string fileName)
        {
            string name = Path.GetFileNameWithoutExtension(fileName);
            foreach (ITemplateConfiguration templateConfiguration in this.Templates)
            {
                string[] namePartsArray = name.ToUpper().Split(templateConfiguration.FileNamePartSeparator);

                if (templateConfiguration.FileNameParts.All(x => namePartsArray.Contains(x.ToUpper())))
                {
                    return templateConfiguration;
                }
            }

            return Templates.Where(x => x.FileNameParts.Contains("*")).FirstOrDefault();
        }

        public IUserConfiguration Clone()
        {
            var configuration = new UserSMTPConfiguration();

            configuration.FtpInterval = this.FtpInterval;
            configuration.HasDeleteFtp = this.HasDeleteFtp;
            configuration.Name = this.Name;
            configuration.SmtpUser = this.SmtpUser;
            configuration.SmtpPass = this.SmtpPass;
            configuration.TemplateFilePath = this.TemplateFilePath;
            configuration.UserGMT = this.UserGMT;
            configuration.AttachmentsFolder = this.AttachmentsFolder;
            configuration.MaxParallelProcessors = this.MaxParallelProcessors;

            if (this.Errors != null)
            {
                configuration.Errors = Errors.Clone();
            }

            if (this.Results != null)
            {
                configuration.Results = this.Results.Clone();
            }

            if (this.AdminEmail != null)
            {
                configuration.AdminEmail = this.AdminEmail.Clone();
            }

            if (this.DownloadFolders != null)
            {
                configuration.DownloadFolders = new List<string>();

                foreach (string downloadFolder in this.DownloadFolders)
                {
                    configuration.DownloadFolders.Add(downloadFolder);
                }
            }

            if (this.FileExtensions != null)
            {
                configuration.FileExtensions = new List<string>();
                foreach (string fileExtension in this.FileExtensions)
                {
                    configuration.FileExtensions.Add(fileExtension);
                }
            }

            if (this.Ack != null)
            {
                configuration.Ack = this.Ack.Clone();
            }

            if (this.Credentials != null)
            {
                configuration.Credentials = this.Credentials.Clone();
            }

            if (this.Ftp != null)
            {
                configuration.Ftp = this.Ftp.Clone();
            }

            if (this.Reports != null)
            {
                configuration.Reports = this.Reports.Clone();
            }

            if (this.Alerts != null)
            {
                configuration.Alerts = this.Alerts.Clone();
            }

            if (this.Templates != null)
            {
                configuration.Templates = new List<ITemplateConfiguration>();

                foreach (ITemplateConfiguration template in this.Templates)
                {
                    configuration.Templates.Add(template.Clone());
                }
            }

            return configuration;
        }

        public PreProcessor GetPreProcessor(ILog logger, IConfiguration configuration)
        {
            return PreProcessor.GetPreProcessor(logger, configuration);
        }
    }
}
