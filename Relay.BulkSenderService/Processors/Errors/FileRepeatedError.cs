using Relay.BulkSenderService.Configuration;
using System;
using System.IO;

namespace Relay.BulkSenderService.Processors.Errors
{
    public class FileRepeatedError : Error
    {
        public FileRepeatedError(IConfiguration configuration) : base(configuration)
        {

        }

        protected override string GetBody()
        {
            return File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorRepeated.es.html");
        }
    }
}
