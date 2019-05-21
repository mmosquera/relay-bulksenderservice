using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors.PreProcess;

namespace Relay.BulkSenderService.Configuration
{
    public interface IPreProcessorConfiguration
    {
        PreProcessor GetPreProcessor(ILog logger, IConfiguration configuration);
    }
}
