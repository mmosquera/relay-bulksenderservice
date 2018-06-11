using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public abstract class ReportTypeConfiguration
    {
        public int Hour { get; set; }
        public IReportName Name { get; set; }
        public char FieldSeparator { get; set; }
        public List<string> Templates { get; set; }
        public string DateFormat { get; set; }
        public List<ReportItemConfiguration> ReportItems { get; set; }
        public List<ReportFieldConfiguration> ReportFields { get; set; }

        public abstract ReportTypeConfiguration Clone();

        public abstract ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger);
    }
}
