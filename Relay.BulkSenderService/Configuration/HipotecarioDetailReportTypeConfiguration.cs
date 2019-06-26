using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;

namespace Relay.BulkSenderService.Configuration
{
    public class HipotecarioDetailReportTypeConfiguration : DailyReportTypeConfiguration
    {
        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new HipotecarioDetailReportProcessor(logger, configuration, this);
        }
    }
}
