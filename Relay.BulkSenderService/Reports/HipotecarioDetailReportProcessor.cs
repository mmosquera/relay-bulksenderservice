using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;

namespace Relay.BulkSenderService.Reports
{
    public class HipotecarioDetailReportProcessor : HipotecarioReportProcessor
    {
        public HipotecarioDetailReportProcessor(ILog logger, IConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {
        }
    }
}
