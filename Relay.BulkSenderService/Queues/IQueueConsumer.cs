using Relay.BulkSenderService.Configuration;
using System;
using System.Threading;

namespace Relay.BulkSenderService.Queues
{
    public interface IQueueConsumer
    {
        event EventHandler<QueueResultEventArgs> ResultEvent;
        event EventHandler<QueueErrorEventArgs> ErrorEvent;

        void ProcessMessages(IConfiguration configuration, IUserConfiguration userConfiguration, IBulkQueue queue, CancellationToken cancellationToken);
    }
}
