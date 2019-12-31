using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;

namespace Relay.BulkSenderService.Configuration
{
    public class FullDailyReportTypeConfiguration : DailyReportTypeConfiguration
    {
        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new FullDailyReportProcessor(logger, configuration, this);
        }
    }
}
