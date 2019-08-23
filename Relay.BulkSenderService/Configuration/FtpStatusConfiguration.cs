using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors.Status;

namespace Relay.BulkSenderService.Configuration
{
    public class FtpStatusConfiguration : IStatusConfiguration
    {
        public int LastViewingHours { get; set; }
        public int MinutesToRefresh { get; set; }

        public StatusProcessor GetStatusProcessor(ILog logger, IConfiguration configuration)
        {
            return new FtpStatusProcessor(logger, configuration);
        }
    }
}