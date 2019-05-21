using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration.Alerts;
using Relay.BulkSenderService.Processors;
using Relay.BulkSenderService.Processors.PreProcess;
using System;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public interface IUserConfiguration
    {
        string Name { get; set; }
        List<string> DownloadFolders { get; set; }
        string AttachmentsFolder { get; set; }
        int FtpInterval { get; set; }
        List<string> FileExtensions { get; set; }
        ErrorConfiguration Errors { get; set; }
        bool HasDeleteFtp { get; set; }
        IResultConfiguration Results { get; set; }
        int UserGMT { get; set; }
        AckConfiguration Ack { get; set; }
        CredentialsConfiguration Credentials { get; set; }
        IFtpConfiguration Ftp { get; set; }
        ReportConfiguration Reports { get; set; }
        AlertConfiguration Alerts { get; set; }
        IPreProcessorConfiguration PreProcessor { get; set; }

        Processor GetProcessor(ILog logger, IConfiguration configuration, string fileName); // TODO get from container.

        ITemplateConfiguration GetTemplateConfiguration(string fileName);

        DateTimeOffset GetUserDateTime();

        IUserConfiguration Clone();

        PreProcessor GetPreProcessor(ILog logger, IConfiguration configuration);
    }
}
