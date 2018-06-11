using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class EdesurReportTypeConfiguration : ReportTypeConfiguration
    {
        public override ReportTypeConfiguration Clone()
        {
            var edesurReportConfiguration = new EdesurReportTypeConfiguration();

            edesurReportConfiguration.Hour = this.Hour;
            edesurReportConfiguration.DateFormat = this.DateFormat;

            if (this.Name != null)
            {
                edesurReportConfiguration.Name = this.Name.Clone();
            }

            if (this.ReportFields != null)
            {
                edesurReportConfiguration.ReportFields = new List<ReportFieldConfiguration>();
                foreach (ReportFieldConfiguration field in this.ReportFields)
                {
                    edesurReportConfiguration.ReportFields.Add(field.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                edesurReportConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    edesurReportConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                edesurReportConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    edesurReportConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            return edesurReportConfiguration;
        }

        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new EdesurReportProcessor(configuration, logger, this);
        }
    }
}
