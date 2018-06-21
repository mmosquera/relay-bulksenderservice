using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors;
using Relay.BulkSenderService.Reports;
using System;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class FileReportTypeConfiguration : ReportTypeConfiguration
    {
        public override ReportTypeConfiguration Clone()
        {
            var fileReportConfiguration = new FileReportTypeConfiguration();

            fileReportConfiguration.ReportId = this.ReportId;
            fileReportConfiguration.OffsetHour = this.OffsetHour;
            fileReportConfiguration.RunHour = this.RunHour;
            fileReportConfiguration.DateFormat = this.DateFormat;

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

        public override ReportExecution GetReportExecution(IUserConfiguration user, ReportExecution reportExecution)
        {
            if (reportExecution != null)
            {
                reportExecution.LastRun = reportExecution.NextRun;
                reportExecution.NextRun = reportExecution.NextRun.AddHours(1);
            }
            else
            {
                DateTime nextRun = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0).AddHours(1);

                reportExecution = new ReportExecution()
                {
                    UserName = user.Name,
                    ReportId = this.ReportId,
                    NextRun = nextRun,
                    LastRun = nextRun.AddHours(-1)
                };
            }

            return reportExecution;
        }

        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new FileReportProcessor(configuration, logger, this);
        }
    }
}
