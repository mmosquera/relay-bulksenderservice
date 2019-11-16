using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors.Acknowledgement;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class GireAckConfiguration : IAckConfiguration
    {
        public List<string> Extensions { get; set; }

        public IAckProcessor GetAckProcessor(ILog logger, IConfiguration configuration)
        {
            return new GireAckProcessor(logger, configuration);
        }
    }
}