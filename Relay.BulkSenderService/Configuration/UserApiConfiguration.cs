using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration.Alerts;
using Relay.BulkSenderService.Processors;
using Relay.BulkSenderService.Processors.Acknowledgement;
using Relay.BulkSenderService.Processors.PreProcess;
using Relay.BulkSenderService.Processors.Status;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Configuration
{
    public class UserApiConfiguration : IUserConfiguration
    {
        public int FtpInterval { get; set; }
        public bool HasDeleteFtp { get; set; }
        public string Name { get; set; }
        public int UserGMT { get; set; }
        public ErrorConfiguration Errors { get; set; }
        public IResultConfiguration Results { get; set; }
        public List<string> FileExtensions { get; set; }
        public List<string> DownloadFolders { get; set; }
        public string AttachmentsFolder { get; set; }
        public List<ITemplateConfiguration> Templates { get; set; }
        public CredentialsConfiguration Credentials { get; set; }
        public IFtpConfiguration Ftp { get; set; }
        public ReportConfiguration Reports { get; set; }
        public AlertConfiguration Alerts { get; set; }
        public IPreProcessorConfiguration PreProcessor { get; set; }
        public int MaxParallelProcessors { get; set; }
        public int DeliveryDelay { get; set; }
        public int MaxThreadsNumber { get; set; }
        public IStatusConfiguration Status { get; set; }
        public IAckConfiguration Ack { get; set; }

        public DateTimeOffset GetUserDateTime()
        {
            var timeSpan = new TimeSpan(UserGMT, 0, 0);

            return new DateTimeOffset(DateTimeOffset.UtcNow.Add(timeSpan).DateTime, timeSpan);
        }

        public Processor GetProcessor(ILog logger, IConfiguration configuration, string fileName)
        {
            return GetTemplateConfiguration(fileName)?.GetProcessor(logger, configuration);
        }

        public ITemplateConfiguration GetTemplateConfiguration(string fileName)
        {
            string name = Path.GetFileNameWithoutExtension(fileName);

            var orderedTemplates = Templates.Where(x => !x.FileNameParts.Contains("*"))
                .OrderByDescending(x => x.FileNameParts.Count)
                .ThenByDescending(x => x.FileNameParts.Max(y => y.Length));

            foreach (ITemplateConfiguration templateConfiguration in orderedTemplates)
            {
                if (templateConfiguration.FileNameParts.All(x => name.Contains(x, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return templateConfiguration;
                }
            }

            return Templates.FirstOrDefault(x => x.FileNameParts.Contains("*"));
        }

        public PreProcessor GetPreProcessor(ILog logger, IConfiguration configuration, string fileName)
        {
            PreProcessor preProcessor = GetTemplateConfiguration(fileName)?.PreProcessor?.GetPreProcessor(logger, configuration);

            if (preProcessor == null)
            {
                preProcessor = PreProcessor.GetPreProcessor(logger, configuration);
            }

            return preProcessor;
        }

        public StatusProcessor GetStatusProcessor(ILog logger, IConfiguration configuration)
        {
            return Status.GetStatusProcessor(logger, configuration);
        }

        public IAckProcessor GetAckProcessor(ILog logger, IConfiguration configuration)
        {
            return Ack.GetAckProcessor(logger, configuration);
        }
    }
}
