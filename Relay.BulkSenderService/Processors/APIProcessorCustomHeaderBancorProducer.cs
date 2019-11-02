using Relay.BulkSenderService.Configuration;
using System.Linq;

namespace Relay.BulkSenderService.Processors
{
    public class ApiProcessorCustomHeaderBancorProducer : ApiProcessorProducer
    {
        public ApiProcessorCustomHeaderBancorProducer(IConfiguration configuration) : base(configuration)
        {
        }

        protected override string[] GetDataLine(string line, ITemplateConfiguration templateConfiguration)
        {
            string[] lineArray = line.Split(templateConfiguration.FieldSeparator);

            var charsToTrim = new char[] { '"' };

            return lineArray.Select(x => x.Trim().Trim(charsToTrim)).ToArray();
        }
    }
}
