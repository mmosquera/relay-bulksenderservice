using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;

namespace Relay.BulkSenderService.Configuration
{
    public class HipotecarioClicksReportTypeConfiguration : DailyReportTypeConfiguration
    {
        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new HipotecarioClicksReportProcessor(logger, configuration, this);
        }
    }
}
