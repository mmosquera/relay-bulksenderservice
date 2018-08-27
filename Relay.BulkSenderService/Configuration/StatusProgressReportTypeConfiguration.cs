using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class StatusProgressReportTypeConfiguration : ReportTypeConfiguration
    {
        public override ReportTypeConfiguration Clone()
        {
            var hipotecarioCabeceraReportTypeConfiguration = new RicohStatusReportTypeConfiguration();

            hipotecarioCabeceraReportTypeConfiguration.ReportId = this.ReportId;
            hipotecarioCabeceraReportTypeConfiguration.OffsetHour = this.OffsetHour;
            hipotecarioCabeceraReportTypeConfiguration.RunHour = this.RunHour;
            hipotecarioCabeceraReportTypeConfiguration.DateFormat = this.DateFormat;

            if (this.Name != null)
            {
                hipotecarioCabeceraReportTypeConfiguration.Name = this.Name.Clone();
            }

            if (this.ReportFields != null)
            {
                hipotecarioCabeceraReportTypeConfiguration.ReportFields = new List<ReportFieldConfiguration>();
                foreach (ReportFieldConfiguration field in this.ReportFields)
                {
                    hipotecarioCabeceraReportTypeConfiguration.ReportFields.Add(field.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                hipotecarioCabeceraReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    hipotecarioCabeceraReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                hipotecarioCabeceraReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    hipotecarioCabeceraReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            return hipotecarioCabeceraReportTypeConfiguration;
        }

        public override ReportExecution GetReportExecution(IUserConfiguration user, ReportExecution reportExecution)
        {
            if (reportExecution != null)
            {
                reportExecution.LastRun = reportExecution.NextRun;
                reportExecution.NextRun = reportExecution.NextRun.AddHours(this.RunHour);
            }
            else
            {
                DateTime nextRun = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);

                nextRun = nextRun.AddHours(this.RunHour);

                reportExecution = new ReportExecution()
                {
                    UserName = user.Name,
                    ReportId = this.ReportId,
                    NextRun = nextRun,
                    LastRun = nextRun.AddHours(-this.RunHour)
                };
            }

            return reportExecution;
        }

        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new StatusProgressReportProcessor(logger, configuration, this);
        }
    }
}
