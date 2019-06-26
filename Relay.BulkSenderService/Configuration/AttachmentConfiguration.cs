using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class AttachmentConfiguration
    {
        public string Folder { get; set; }
        public List<FieldConfiguration> Fields { get; set; }
    }
}
