using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;

namespace Relay.BulkSenderService.Processors.Status
{
    public abstract class StatusProcessor
    {
        protected readonly ILog _logger;
        protected readonly IConfiguration _configuration;

        public StatusProcessor(ILog logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public abstract void ProcessStatusFile(IUserConfiguration userConfiguration, string fileName);
    }
}
