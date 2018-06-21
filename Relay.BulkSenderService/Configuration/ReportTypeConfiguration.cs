using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors;
using Relay.BulkSenderService.Reports;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public abstract class ReportTypeConfiguration
    {
        public string ReportId { get; set; }
        public int OffsetHour { get; set; }
        public int RunHour { get; set; }
        public IReportName Name { get; set; }
        public char FieldSeparator { get; set; }
        public List<string> Templates { get; set; }
        public string DateFormat { get; set; }
        public List<ReportItemConfiguration> ReportItems { get; set; }
        public List<ReportFieldConfiguration> ReportFields { get; set; }

        public abstract ReportTypeConfiguration Clone();

        public abstract ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger);

        public abstract ReportExecution GetReportExecution(IUserConfiguration user, ReportExecution reportExecution);
    }
}
