using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
	public class CsvFileReportTypeConfiguration : FileReportTypeConfiguration
	{
		public override ReportTypeConfiguration Clone()
		{
			var fileReportConfiguration = new CsvFileReportTypeConfiguration()
			{
				ReportId = this.ReportId,
				OffsetHour = this.OffsetHour,
				RunHour = this.RunHour,
				DateFormat = this.DateFormat
			};

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

		public override List<ReportExecution> GetReportExecution(IUserConfiguration user, ReportExecution lastExecution)
		{
			return new List<ReportExecution>();
		}

		public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
		{
			return new CsvFileReportProcessor(configuration, logger, this);
		}
	}
}
