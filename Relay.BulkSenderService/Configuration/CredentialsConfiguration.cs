namespace Relay.BulkSenderService.Configuration
{
    public class CredentialsConfiguration
    {
        public int AccountId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ApiKey { get; set; }

        public CredentialsConfiguration Clone()
        {
            var credentialsConfiguration = new CredentialsConfiguration();

            credentialsConfiguration.AccountId = this.AccountId;
            credentialsConfiguration.Email = this.Email;
            credentialsConfiguration.Password = this.Password;
            credentialsConfiguration.ApiKey = this.ApiKey;

            return credentialsConfiguration;
        }
    }    
}
