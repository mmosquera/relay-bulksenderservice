using Relay.BulkSenderService.Processors;
using System;

namespace Relay.BulkSenderService.Queues
{
    public class QueueErrorEventArgs : QueueEventArgs
    {
        public ErrorType Type { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }
}