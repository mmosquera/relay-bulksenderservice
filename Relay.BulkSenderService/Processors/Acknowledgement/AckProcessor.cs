using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;

namespace Relay.BulkSenderService.Processors.Acknowledgement
{
    public abstract class AckProcessor : IAckProcessor
    {
        protected readonly ILog _logger;
        protected readonly IConfiguration _configuration;

        public AckProcessor(ILog logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        public abstract void ProcessAckFile(string fileName);
    }
}
