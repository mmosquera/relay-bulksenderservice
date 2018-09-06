using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors;

namespace Relay.BulkSenderService.Configuration
{
    public class TemplateBanorteConfiguration : BaseTemplateConfiguration
    {
        public override Processor GetProcessor(ILog logger, IConfiguration configuration)
        {
            return new APIProcessorBanorte(logger, configuration);
        }
    }
}
