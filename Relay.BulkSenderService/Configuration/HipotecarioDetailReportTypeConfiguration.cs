using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class HipotecarioDetailReportTypeConfiguration : DailyReportTypeConfiguration
    {
        public override ReportTypeConfiguration Clone()
        {
            var hipotecarioDetailReportTypeConfiguration = new HipotecarioDetailReportTypeConfiguration();

            hipotecarioDetailReportTypeConfiguration.ReportId = this.ReportId;
            hipotecarioDetailReportTypeConfiguration.OffsetHour = this.OffsetHour;
            hipotecarioDetailReportTypeConfiguration.RunHour = this.RunHour;
            hipotecarioDetailReportTypeConfiguration.DateFormat = this.DateFormat;

            if (this.Name != null)
            {
                hipotecarioDetailReportTypeConfiguration.Name = this.Name.Clone();
            }

            if (this.ReportFields != null)
            {
                hipotecarioDetailReportTypeConfiguration.ReportFields = new List<ReportFieldConfiguration>();
                foreach (ReportFieldConfiguration field in this.ReportFields)
                {
                    hipotecarioDetailReportTypeConfiguration.ReportFields.Add(field.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                hipotecarioDetailReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    hipotecarioDetailReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                hipotecarioDetailReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    hipotecarioDetailReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            return hipotecarioDetailReportTypeConfiguration;
        }

        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new HipotecarioDetailReportProcessor(logger, configuration, this);
        }
    }
}
