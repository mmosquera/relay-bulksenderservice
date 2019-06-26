using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;

namespace Relay.BulkSenderService.Configuration
{
    public class HipotecarioCabeceraReportTypeConfiguration : DailyReportTypeConfiguration
    {
        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new HipotecarioCabeceraReportProcessor(logger, configuration, this);
        }
    }
}
