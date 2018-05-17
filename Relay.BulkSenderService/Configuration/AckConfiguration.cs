using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class AckConfiguration
    {
        public List<string> FileExtensions { get; set; }

        public AckConfiguration Clone()
        {
            var ackConfiguration = new AckConfiguration();

            if (this.FileExtensions != null)
            {
                ackConfiguration.FileExtensions = new List<string>();

                foreach (string fileExtension in this.FileExtensions)
                {
                    ackConfiguration.FileExtensions.Add(fileExtension);
                }
            }

            return ackConfiguration;
        }
    }
}
