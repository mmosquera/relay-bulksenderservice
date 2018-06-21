using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class EdesurReportTypeConfiguration : ReportTypeConfiguration
    {
        public override ReportTypeConfiguration Clone()
        {
            var edesurReportTypeConfiguration = new EdesurReportTypeConfiguration();

            edesurReportTypeConfiguration.ReportId = this.ReportId;
            edesurReportTypeConfiguration.OffsetHour = this.OffsetHour;
            edesurReportTypeConfiguration.RunHour = this.RunHour;
            edesurReportTypeConfiguration.DateFormat = this.DateFormat;

            if (this.Name != null)
            {
                edesurReportTypeConfiguration.Name = this.Name.Clone();
            }

            if (this.ReportFields != null)
            {
                edesurReportTypeConfiguration.ReportFields = new List<ReportFieldConfiguration>();
                foreach (ReportFieldConfiguration field in this.ReportFields)
                {
                    edesurReportTypeConfiguration.ReportFields.Add(field.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                edesurReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    edesurReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                edesurReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    edesurReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            return edesurReportTypeConfiguration;
        }

        public override ReportExecution GetReportExecution(IUserConfiguration user, ReportExecution reportExecution)
        {
            if (reportExecution != null)
            {
                reportExecution.LastRun = reportExecution.NextRun;
                reportExecution.NextRun = reportExecution.NextRun.AddHours(3);
            }
            else
            {
                DateTime nextRun = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0).AddHours(3);

                reportExecution = new ReportExecution()
                {
                    UserName = user.Name,
                    ReportId = this.ReportId,
                    NextRun = nextRun,
                    LastRun = nextRun.AddHours(-3)
                };
            }

            return reportExecution;
        }

        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new EdesurReportProcessor(configuration, logger, this);
        }
    }
}
