using System;

namespace Relay.BulkSenderService.Classes
{
    public class ReportExecution
    {
        public string UserName { get; set; }
        public string ReportId { get; set; }
        public DateTime LastRun { get; set; }
        public DateTime NextRun { get; set; }
    }
}