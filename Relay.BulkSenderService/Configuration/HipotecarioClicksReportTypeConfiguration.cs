using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class HipotecarioClicksReportTypeConfiguration : ReportTypeConfiguration
    {
        public override ReportTypeConfiguration Clone()
        {
            var hipotecarioClicksReportTypeConfiguration = new HipotecarioClicksReportTypeConfiguration();

            hipotecarioClicksReportTypeConfiguration.Hour = this.Hour;
            hipotecarioClicksReportTypeConfiguration.DateFormat = this.DateFormat;

            if (this.Name != null)
            {
                hipotecarioClicksReportTypeConfiguration.Name = this.Name.Clone();
            }

            if (this.ReportFields != null)
            {
                hipotecarioClicksReportTypeConfiguration.ReportFields = new List<ReportFieldConfiguration>();
                foreach (ReportFieldConfiguration field in this.ReportFields)
                {
                    hipotecarioClicksReportTypeConfiguration.ReportFields.Add(field.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                hipotecarioClicksReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    hipotecarioClicksReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                hipotecarioClicksReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    hipotecarioClicksReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            return hipotecarioClicksReportTypeConfiguration;
        }

        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new HipotecarioClicksReportProcessor(logger, configuration, this);
        }
    }
}
