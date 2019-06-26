using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class AdminEmailConfiguration
    {
        public List<string> Emails { get; set; }
        public bool HasStartEmail { get; set; }
        public bool HasEndEmail { get; set; }
        public bool HasErrorEmail { get; set; }
    }
}
