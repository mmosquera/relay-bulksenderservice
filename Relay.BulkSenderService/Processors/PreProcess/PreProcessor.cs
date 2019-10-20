using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public abstract class PreProcessor
    {
        protected readonly ILog _logger;
        protected readonly IConfiguration _configuration;

        public PreProcessor(ILog logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public abstract void ProcessFile(string fileName, IUserConfiguration userConfiguration);
    }
}
