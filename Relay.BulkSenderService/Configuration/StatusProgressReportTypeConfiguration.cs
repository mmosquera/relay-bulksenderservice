using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class StatusProgressReportTypeConfiguration : ReportTypeConfiguration
    {
        public override List<ReportExecution> GetReportExecution(IUserConfiguration user, ReportExecution lastExecution)
        {
            var reports = new List<ReportExecution>();

            DateTime nextRun, lastRun;

            if (lastExecution != null)
            {
                lastRun = lastExecution.NextRun;
                nextRun = lastExecution.NextRun.AddHours(this.RunHour);
            }
            else
            {
                DateTime today = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);

                nextRun = today.AddHours(this.RunHour);
                lastRun = nextRun.AddHours(-this.RunHour);
            }

            DateTime nextDay = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0).AddDays(1);

            var execution = new ReportExecution()
            {
                ReportId = this.ReportId,
                NextRun = nextRun,
                LastRun = lastRun,
                Processed = false,
                RunDate = nextRun,
                CreatedAt = DateTime.UtcNow,
            };

            while (execution.NextRun < nextDay)
            {
                reports.Add(execution);

                nextRun = execution.NextRun.AddHours(this.RunHour);
                lastRun = execution.NextRun;

                execution = new ReportExecution()
                {
                    ReportId = this.ReportId,
                    NextRun = nextRun,
                    LastRun = lastRun,
                    Processed = false,
                    RunDate = nextRun,
                    CreatedAt = DateTime.UtcNow
                };
            }

            return reports;
        }

        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new StatusProgressReportProcessor(logger, configuration, this);
        }
    }
}
