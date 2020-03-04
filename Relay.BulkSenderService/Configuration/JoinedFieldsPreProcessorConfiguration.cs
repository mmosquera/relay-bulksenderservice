using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors.PreProcess;

namespace Relay.BulkSenderService.Configuration
{
    public class JoinedFieldsPreProcessorConfiguration : IPreProcessorConfiguration
    {
        public PreProcessor GetPreProcessor(ILog logger, IConfiguration configuration)
        {
            return new JoinedFieldsPreProcessor(logger, configuration);
        }
    }
}
