namespace Relay.BulkSenderService.Queues
{
    public class BulkQueueMessage : IBulkQueueMessage
    {
        public int LineNumber { get; set; }
        public string Message { get; set; }
    }
}
