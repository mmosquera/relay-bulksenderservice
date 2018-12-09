﻿using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
	public class StatusProgressReportTypeConfiguration : ReportTypeConfiguration
	{
		public override ReportTypeConfiguration Clone()
		{
			var hipotecarioCabeceraReportTypeConfiguration = new RicohStatusReportTypeConfiguration()
			{
				ReportId = this.ReportId,
				OffsetHour = this.OffsetHour,
				RunHour = this.RunHour,
				DateFormat = this.DateFormat
			};

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
				UserName = user.Name,
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
					UserName = user.Name,
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
