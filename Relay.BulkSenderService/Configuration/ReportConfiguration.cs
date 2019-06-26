using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class ReportConfiguration
    {
        public string Folder { get; set; }
        public List<ReportTypeConfiguration> ReportsList { get; set; }
    }
}
