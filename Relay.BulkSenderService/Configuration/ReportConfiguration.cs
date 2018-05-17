using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class ReportConfiguration
    {
        public string Folder { get; set; }
        public List<ReportTypeConfiguration> ReportsList { get; set; }

        public ReportConfiguration Clone()
        {
            var reportConfiguration = new ReportConfiguration();

            reportConfiguration.Folder = this.Folder;

            if (this.ReportsList != null)
            {
                reportConfiguration.ReportsList = new List<ReportTypeConfiguration>();
                foreach (ReportTypeConfiguration reportTypeConfiguration in this.ReportsList)
                {
                    reportConfiguration.ReportsList.Add(reportTypeConfiguration.Clone());
                }
            }

            return reportConfiguration;
        }
    }
}
