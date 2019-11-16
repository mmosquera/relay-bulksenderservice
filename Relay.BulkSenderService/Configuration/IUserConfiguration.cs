using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration.Alerts;
using Relay.BulkSenderService.Processors;
using Relay.BulkSenderService.Processors.Acknowledgement;
using Relay.BulkSenderService.Processors.PreProcess;
using Relay.BulkSenderService.Processors.Status;
using System;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public interface IUserConfiguration
    {
        string Name { get; set; }
        int FtpInterval { get; set; }
        List<string> FileExtensions { get; set; }
        List<string> DownloadFolders { get; set; }
        string AttachmentsFolder { get; set; }
        ErrorConfiguration Errors { get; set; }
        bool HasDeleteFtp { get; set; }
        IResultConfiguration Results { get; set; }
        int UserGMT { get; set; }
        int MaxParallelProcessors { get; set; }
        int DeliveryDelay { get; set; }
        int MaxThreadsNumber { get; set; }
        CredentialsConfiguration Credentials { get; set; }
        List<ITemplateConfiguration> Templates { get; set; }
        IFtpConfiguration Ftp { get; set; }
        ReportConfiguration Reports { get; set; }
        AlertConfiguration Alerts { get; set; }
        IPreProcessorConfiguration PreProcessor { get; set; }
        IStatusConfiguration Status { get; set; }
        IAckConfiguration Ack { get; set; }

        Processor GetProcessor(ILog logger, IConfiguration configuration, string fileName);

        ITemplateConfiguration GetTemplateConfiguration(string fileName);

        DateTimeOffset GetUserDateTime();

        PreProcessor GetPreProcessor(ILog logger, IConfiguration configuration, string fileName);

        StatusProcessor GetStatusProcessor(ILog logger, IConfiguration configuration);

        IAckProcessor GetAckProcessor(ILog logger, IConfiguration configuration);
    }
}