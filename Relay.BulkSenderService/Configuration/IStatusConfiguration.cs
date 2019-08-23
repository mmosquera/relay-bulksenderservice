using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors.Status;

namespace Relay.BulkSenderService.Configuration
{
    public interface IStatusConfiguration
    {
        int LastViewingHours { get; set; }
        int MinutesToRefresh { get; set; }
        StatusProcessor GetStatusProcessor(ILog logger, IConfiguration configuration);
    }
}