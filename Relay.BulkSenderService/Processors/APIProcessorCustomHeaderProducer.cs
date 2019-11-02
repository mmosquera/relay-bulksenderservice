using Relay.BulkSenderService.Configuration;

namespace Relay.BulkSenderService.Processors
{
    public class ApiProcessorCustomHeaderProducer : ApiProcessorProducer
    {
        public ApiProcessorCustomHeaderProducer(IConfiguration configuration) : base(configuration)
        {

        }

        protected override string[] GetDataLine(string line, ITemplateConfiguration templateConfiguration)
        {
            string newLine = string.Empty;

            int start = 0;

            string value;

            foreach (FieldConfiguration field in templateConfiguration.Fields)
            {
                if (start + field.Length <= line.Length)
                {
                    value = line.Substring(start, field.Length);
                    newLine += $"{value.Trim()}{templateConfiguration.FieldSeparator}";
                }
                else
                {
                    newLine += $"{templateConfiguration.FieldSeparator}";
                }
                start += field.Length;
            }

            if (newLine.EndsWith(templateConfiguration.FieldSeparator.ToString()))
            {
                newLine = newLine.Remove(newLine.Length - 1, 1);
            }

            return newLine.Split(templateConfiguration.FieldSeparator);
        }
    }
}
