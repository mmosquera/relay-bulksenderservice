using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Reports
{
    public class HipotecarioDetailReportProcessor : HipotecarioReportProcessor
    {
        public HipotecarioDetailReportProcessor(ILog logger, IConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {
        }

        public override bool GenerateForcedReport(List<string> files, IUserConfiguration user)
        {
            return true;
        }
    }
}
