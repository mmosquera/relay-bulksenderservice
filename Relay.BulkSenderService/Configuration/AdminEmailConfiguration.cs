using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class AdminEmailConfiguration
    {
        public List<string> Emails { get; set; }
        public bool HasStartEmail { get; set; }
        public bool HasEndEmail { get; set; }
        public bool HasErrorEmail { get; set; }

        public AdminEmailConfiguration Clone()
        {
            var adminEmailConfiguration = new AdminEmailConfiguration();

            adminEmailConfiguration.HasStartEmail = this.HasStartEmail;
            adminEmailConfiguration.HasEndEmail = this.HasEndEmail;
            adminEmailConfiguration.HasErrorEmail = this.HasErrorEmail;

            if (this.Emails != null)
            {
                adminEmailConfiguration.Emails = new List<string>();

                foreach (string email in this.Emails)
                {
                    // TODO: review if is copy
                    adminEmailConfiguration.Emails.Add(email);
                }
            }

            return adminEmailConfiguration;
        }
    }
}
