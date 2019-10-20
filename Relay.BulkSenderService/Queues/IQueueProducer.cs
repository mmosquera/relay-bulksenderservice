using Relay.BulkSenderService.Configuration;
using System;
using System.Threading;

namespace Relay.BulkSenderService.Queues
{
    public interface IQueueProducer
    {
        event EventHandler<QueueErrorEventArgs> ErrorEvent;

        void GetMessages(IConfiguration configuration, IUserConfiguration userConfiguration, IBulkQueue queue, string localFileName, CancellationToken cancellationToken);
    }
}
