namespace Relay.BulkSenderService.Queues
{
    interface IBulkQueue
    {
        void SendMessage(IBulkQueueMessage bulkQueueMessage);

        IBulkQueueMessage ReceiveMessage(int waitSeconds = 0);
    }
}
