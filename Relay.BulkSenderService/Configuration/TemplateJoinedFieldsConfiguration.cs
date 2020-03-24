using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors;

namespace Relay.BulkSenderService.Configuration
{
    public class TemplateJoinedFieldsConfiguration : BaseTemplateConfiguration
    {
        public override Processor GetProcessor(ILog logger, IConfiguration configuration)
        {
            return new ApiProcessorJoinedFields(logger, configuration);
        }
    }
}
