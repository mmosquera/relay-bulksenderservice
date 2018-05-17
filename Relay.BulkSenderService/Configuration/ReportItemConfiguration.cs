using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class ReportItemConfiguration
    {
        public int Row { get; set; }
        public List<string> Values { get; set; }

        public ReportItemConfiguration Clone()
        {
            var reportItemConfiguration = new ReportItemConfiguration();
            reportItemConfiguration.Row = this.Row;

            if (this.Values != null)
            {
                reportItemConfiguration.Values = new List<string>();
                foreach (string value in this.Values)
                {
                    reportItemConfiguration.Values.Add(value);
                }
            }

            return reportItemConfiguration;
        }
    }
}
