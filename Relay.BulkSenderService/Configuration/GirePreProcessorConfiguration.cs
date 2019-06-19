using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors.PreProcess;

namespace Relay.BulkSenderService.Configuration
{
    public class GirePreProcessorConfiguration : IPreProcessorConfiguration
    {
        public PreProcessor GetPreProcessor(ILog logger, IConfiguration configuration)
        {
            return new GirePreProcessor(logger, configuration);
        }
    }
}
