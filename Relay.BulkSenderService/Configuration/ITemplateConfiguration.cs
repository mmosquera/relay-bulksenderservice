using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public interface ITemplateConfiguration
    {
        char FileNamePartSeparator { get; set; }
        char FieldSeparator { get; set; }
        string TemplateId { get; set; }
        string TemplateName { get; set; }
        bool HasHeaders { get; set; }
        bool AllowDuplicates { get; set; }
        List<string> FileNameParts { get; set; }
        List<FieldConfiguration> Fields { get; set; }

        ITemplateConfiguration Clone();

        Processor GetProcessor(ILog logger, IConfiguration configuration);
    }
}
