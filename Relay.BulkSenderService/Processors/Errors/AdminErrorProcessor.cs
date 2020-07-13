using Relay.BulkSenderService.Configuration;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Processors.Errors
{
    public class AdminErrorProcessor : ErrorProcessor
    {
        public AdminErrorProcessor(IConfiguration configuration) : base(configuration) { }

        public override void ProcessError(IError error)
        {
            var emails = new List<string>() { "leve2support@makingsense.com" };

            new EmailErrorProcessor(_configuration, emails).ProcessError(error);
        }
    }
}
