using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Reports
{
    public class HipotecarioClicksReportProcessor : HipotecarioReportProcessor
    {
        public HipotecarioClicksReportProcessor(ILog logger, IConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {
        }

        public override bool GenerateForcedReport(List<string> files, IUserConfiguration user)
        {
            return true;
        }

        protected override void ProcessFilesForReports(List<string> files, IUserConfiguration user)
        {

        }
    }
}
