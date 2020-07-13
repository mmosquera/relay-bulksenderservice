using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Relay.BulkSenderService.Processors.Errors
{
    public class EmailErrorProcessor : ErrorProcessor
    {
        private readonly IEnumerable<string> _emails;
        public EmailErrorProcessor(IConfiguration configuration, IEnumerable<string> emails) : base(configuration)
        {
            _emails = emails;
        }

        public override void ProcessError(IError error)
        {
            new MailSender(_configuration).SendEmail(
                "support@dopplerrelay.com",
                "Doppler Relay Support",
                _emails.ToList(),
                "BULKSENDER ALERT",
                error.GetDescription());
        }
    }
}
