using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class AttachmentConfiguration
    {
        public string Folder { get; set; }
        public List<FieldConfiguration> Fields { get; set; }

        public AttachmentConfiguration Clone()
        {
            var attachConfiguration = new AttachmentConfiguration();
            attachConfiguration.Folder = this.Folder;

            if (Fields != null)
            {
                attachConfiguration.Fields = new List<FieldConfiguration>();
                foreach (FieldConfiguration fieldConfiguration in this.Fields)
                {
                    attachConfiguration.Fields.Add(fieldConfiguration.Clone());
                }
            }

            return attachConfiguration;
        }
    }
}
