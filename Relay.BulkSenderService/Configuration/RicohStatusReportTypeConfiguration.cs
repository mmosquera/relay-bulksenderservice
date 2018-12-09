using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
	public class RicohStatusReportTypeConfiguration : DailyReportTypeConfiguration
	{
		public override ReportTypeConfiguration Clone()
		{
			var ricohStatusReportTypeConfiguration = new RicohStatusReportTypeConfiguration()
			{
				ReportId = this.ReportId,
				OffsetHour = this.OffsetHour,
				RunHour = this.RunHour,
				DateFormat = this.DateFormat
			};

			if (this.Name != null)
			{
				ricohStatusReportTypeConfiguration.Name = this.Name.Clone();
			}

			if (this.ReportFields != null)
			{
				ricohStatusReportTypeConfiguration.ReportFields = new List<ReportFieldConfiguration>();

				foreach (ReportFieldConfiguration field in this.ReportFields)
				{
					ricohStatusReportTypeConfiguration.ReportFields.Add(field.Clone());
				}
			}

			if (this.ReportItems != null)
			{
				ricohStatusReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();

				foreach (ReportItemConfiguration reportItem in this.ReportItems)
				{
					ricohStatusReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
				}
			}

			if (this.ReportItems != null)
			{
				ricohStatusReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();

				foreach (ReportItemConfiguration reportItem in this.ReportItems)
				{
					ricohStatusReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
				}
			}

			return ricohStatusReportTypeConfiguration;
		}

		public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
		{
			return new RicohStatusReportProcessor(logger, configuration, this);
		}
	}
}
