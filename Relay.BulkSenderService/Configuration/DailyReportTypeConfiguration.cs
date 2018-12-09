using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
	public class DailyReportTypeConfiguration : ReportTypeConfiguration
	{
		public override ReportTypeConfiguration Clone()
		{
			var dailyReportTypeConfiguration = new DailyReportTypeConfiguration()
			{
				ReportId = this.ReportId,
				OffsetHour = this.OffsetHour,
				RunHour = this.RunHour,
				DateFormat = this.DateFormat,
			};

			if (this.Name != null)
			{
				dailyReportTypeConfiguration.Name = this.Name.Clone();
			}

			if (this.ReportFields != null)
			{
				dailyReportTypeConfiguration.ReportFields = new List<ReportFieldConfiguration>();

				foreach (ReportFieldConfiguration field in this.ReportFields)
				{
					dailyReportTypeConfiguration.ReportFields.Add(field.Clone());
				}
			}

			if (this.ReportItems != null)
			{
				dailyReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();

				foreach (ReportItemConfiguration reportItem in this.ReportItems)
				{
					dailyReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
				}
			}

			if (this.ReportItems != null)
			{
				dailyReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();

				foreach (ReportItemConfiguration reportItem in this.ReportItems)
				{
					dailyReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
				}
			}

			return dailyReportTypeConfiguration;
		}

		public override List<ReportExecution> GetReportExecution(IUserConfiguration user, ReportExecution lastExecution)
		{
			DateTime now = DateTime.UtcNow.AddHours(user.UserGMT);

			DateTime nextRun = new DateTime(now.Year, now.Month, now.Day, this.RunHour, 0, 0);

			if (nextRun < now)
			{
				nextRun = nextRun.AddDays(1);
			}

			nextRun = nextRun.AddHours(-user.UserGMT);

			var reportExecution = new ReportExecution()
			{
				UserName = user.Name,
				ReportId = this.ReportId,
				NextRun = nextRun,
				LastRun = nextRun.AddDays(-1),
				RunDate = nextRun,
				Processed = false,
				CreatedAt = DateTime.UtcNow
			};

			return new List<ReportExecution>() { reportExecution };
		}

		public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
		{
			return new DailyReportProcessor(logger, configuration, this);
		}
	}
}
