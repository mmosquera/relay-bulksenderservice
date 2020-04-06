using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public interface ITemplateConfiguration
    {
        List<string> DownloadFolders { get; set; }
        string AttachmentsFolder { get; set; }
        char FieldSeparator { get; set; }
        string TemplateId { get; set; }
        string TemplateName { get; set; }
        bool HasHeaders { get; set; }
        bool AllowDuplicates { get; set; }
        List<string> FileNameParts { get; set; }
        List<FieldConfiguration> Fields { get; set; }
        IPreProcessorConfiguration PreProcessor { get; set; }

        Processor GetProcessor(ILog logger, IConfiguration configuration);
    }
}
