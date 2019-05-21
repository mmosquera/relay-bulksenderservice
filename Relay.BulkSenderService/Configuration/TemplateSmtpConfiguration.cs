using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors;

namespace Relay.BulkSenderService.Configuration
{
    public class TemplateSmtpConfiguration : BaseTemplateConfiguration
    {
        public override Processor GetProcessor(ILog logger, IConfiguration configuration)
        {
            return new SMTPProcessor(logger, configuration);
        }
    }
}
