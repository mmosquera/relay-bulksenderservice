using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Processors;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Relay.BulkSenderService.Queues
{
    public interface IQueueProducer
    {
        event EventHandler<QueueErrorEventArgs> ErrorEvent;

        void GetMessages(IUserConfiguration userConfiguration, IBulkQueue queue, List<ProcessError> errors, List<ProcessResult> results, string localFileName, CancellationToken cancellationToken);
    }
}
