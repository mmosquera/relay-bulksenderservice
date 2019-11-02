using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Queues;

namespace Relay.BulkSenderService.Processors
{
    public class APIProcessorCustomHeader : APIProcessor
    {
        public APIProcessorCustomHeader(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
        }

        protected override IQueueProducer GetProducer()
        {
            IQueueProducer producer = new ApiProcessorCustomHeaderProducer(_configuration);
            producer.ErrorEvent += Processor_ErrorEvent;

            return producer;
        }
    }
}
