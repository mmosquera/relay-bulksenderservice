using Relay.BulkSenderService.Configuration;
using System;

namespace Relay.BulkSenderService.Queues
{
    public interface IQueueConsumer
    {
        event EventHandler<QueueResult> ResultEvent;
        event EventHandler<QueueResult> ErrorEvent;

        void ProcessMessages(IUserConfiguration user, IBulkQueue queue);
    }
}
