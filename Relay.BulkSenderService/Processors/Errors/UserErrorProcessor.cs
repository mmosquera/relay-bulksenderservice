using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Configuration.Alerts;

namespace Relay.BulkSenderService.Processors.Errors
{
    public class UserErrorProcessor : ErrorProcessor
    {
        private readonly AlertConfiguration _alertConfiguration;
        public UserErrorProcessor(IConfiguration configuration, AlertConfiguration alertConfiguration) : base(configuration)
        {
            _alertConfiguration = alertConfiguration;
        }

        public override void ProcessError(IError error)
        {
            new EmailErrorProcessor(_configuration, _alertConfiguration.Emails).ProcessError(error);
        }
    }
}
