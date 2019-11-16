using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors.Acknowledgement;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public interface IAckConfiguration
    {
        List<string> Extensions { get; set; }

        IAckProcessor GetAckProcessor(ILog logger, IConfiguration configuration);
    }
}