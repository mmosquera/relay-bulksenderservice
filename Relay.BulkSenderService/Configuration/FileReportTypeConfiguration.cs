using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class FileReportTypeConfiguration : ReportTypeConfiguration
    {
        public int HoursAfterProcess { get; set; }

        public override ReportTypeConfiguration Clone()
        {
            var fileReportConfiguration = new FileReportTypeConfiguration();

            fileReportConfiguration.HoursAfterProcess = this.HoursAfterProcess;
            fileReportConfiguration.Hour = this.Hour;

            if (this.Name != null)
            {
                fileReportConfiguration.Name = this.Name.Clone();
            }

            if (this.ReportFields != null)
            {
                fileReportConfiguration.ReportFields = new List<ReportFieldConfiguration>();
                foreach (ReportFieldConfiguration field in this.ReportFields)
                {
                    fileReportConfiguration.ReportFields.Add(field.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                fileReportConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    fileReportConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            if (this.Templates != null)
            {
                fileReportConfiguration.Templates = new List<string>();
                foreach (string template in this.Templates)
                {
                    fileReportConfiguration.Templates.Add(template);
                }
            }

            return fileReportConfiguration;
        }

        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new FileReportProcessor(configuration, logger, this);
        }
    }
}
