using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Queues;

namespace Relay.BulkSenderService.Processors
{
    public class ApiProcessorProvincia : APIProcessor
    {
        public ApiProcessorProvincia(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
        }

        protected override IQueueProducer GetProducer()
        {
            IQueueProducer producer = new ApiProcessorProvinciaProducer(_configuration);
            producer.ErrorEvent += Processor_ErrorEvent;

            return producer;
        }
    }
}
