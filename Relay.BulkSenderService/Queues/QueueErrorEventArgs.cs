using Relay.BulkSenderService.Classes;
using System;

namespace Relay.BulkSenderService.Queues
{
    public class QueueErrorEventArgs
    {
        public int LineNumber { get; set; }
        public string Message { get; set; }
        public ErrorType Type { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }
}