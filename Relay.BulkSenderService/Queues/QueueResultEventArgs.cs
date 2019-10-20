namespace Relay.BulkSenderService.Queues
{
    public class QueueResultEventArgs
    {
        public int LineNumber { get; set; }
        public string Message { get; set; }
        public string ResourceId { get; set; }
        public string DeliveryLink { get; set; }
    }
}