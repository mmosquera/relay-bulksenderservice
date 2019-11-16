using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors.PreProcess;

namespace Relay.BulkSenderService.Configuration
{
    public class DuplicatesPreProcessorConfiguration : IPreProcessorConfiguration
    {
        public PreProcessor GetPreProcessor(ILog logger, IConfiguration configuration)
        {
            return new DuplicatesPreProcessor(logger, configuration);
        }
    }
}
