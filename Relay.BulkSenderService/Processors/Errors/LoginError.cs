using Relay.BulkSenderService.Configuration;
using System;
using System.IO;

namespace Relay.BulkSenderService.Processors.Errors
{
    public class LoginError : Error
    {
        public LoginError(IConfiguration configuration) : base(configuration)
        {

        }

        protected override string GetBody()
        {
            return File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorLogin.es.html");
        }
    }
}
