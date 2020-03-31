using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors.PreProcess;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class CredicopPreProcessorConfiguration : IPreProcessorConfiguration
    {
        public List<TemplateMapping> Mappings { get; set; }

        public PreProcessor GetPreProcessor(ILog logger, IConfiguration configuration)
        {
            return new CredicopPreProcessor(logger, configuration, Mappings);
        }
    }

    public class TemplateMapping
    {
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
    }
}
