using Relay.BulkSenderService.Configuration;
using System;

namespace Relay.BulkSenderService.Queues
{
    public interface IQueueProducer
    {
        event EventHandler<QueueResult> ErrorEvent;

        void GetMessages(IUserConfiguration user, IBulkQueue queue, string fileName);
    }
}
