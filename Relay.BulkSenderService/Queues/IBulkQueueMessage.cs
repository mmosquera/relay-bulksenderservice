namespace Relay.BulkSenderService.Queues
{
    public interface IBulkQueueMessage
    {
        int LineNumber { get; set; }
        string Message { get; set; }
    }
}
