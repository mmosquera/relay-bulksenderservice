using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;

namespace Relay.BulkSenderService.Configuration
{
    public class RicohStatusReportTypeConfiguration : DailyReportTypeConfiguration
    {
        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new RicohStatusReportProcessor(logger, configuration, this);
        }
    }
}
