using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors;
using System;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class UserSMTPConfiguration : IUserConfiguration
    {
        public char FieldSeparator { get; set; }
        public int FtpInterval { get; set; }
        public bool HasDeleteFtp { get; set; }
        public string Name { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
        public string TemplateFilePath { get; set; }
        public int UserGMT { get; set; }
        public string AttachFilePath { get; set; }
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

        public Processor GetProcessor(ILog logger, IConfiguration configuration, string fileName)
        {
            return new SMTPProcessor(logger, configuration);
        }

        public DateTimeOffset GetUserDateTime()
        {
            var timeSpan = new TimeSpan(UserGMT, 0, 0);

            return new DateTimeOffset(DateTimeOffset.UtcNow.Add(timeSpan).DateTime, timeSpan);
        }

        public IUserConfiguration Clone()
        {
            var configuration = new UserSMTPConfiguration();

            configuration.AttachFilePath = this.AttachFilePath;
            configuration.FieldSeparator = this.FieldSeparator;
            configuration.FtpInterval = this.FtpInterval;
            configuration.HasDeleteFtp = this.HasDeleteFtp;
            configuration.Name = this.Name;
            configuration.SmtpUser = this.SmtpUser;
            configuration.SmtpPass = this.SmtpPass;
            configuration.TemplateFilePath = this.TemplateFilePath;
            configuration.UserGMT = this.UserGMT;
            configuration.AttachmentsFolder = this.AttachmentsFolder;

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

            return configuration;
        }
    }
}
