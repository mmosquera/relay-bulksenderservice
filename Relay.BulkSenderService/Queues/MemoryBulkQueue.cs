using System;
using System.Collections.Concurrent;

namespace Relay.BulkSenderService.Queues
{
    public class MemoryBulkQueue : IBulkQueue
    {
        private ConcurrentQueue<IBulkQueueMessage> concurrentQueue;

        public MemoryBulkQueue()
        {
            concurrentQueue = new ConcurrentQueue<IBulkQueueMessage>();
        }

        public IBulkQueueMessage ReceiveMessage()
        {
            IBulkQueueMessage message = null;

            concurrentQueue.TryDequeue(out message);

            return message;
        }

        public void SendMessage(IBulkQueueMessage bulkQueueMessage)
        {
            concurrentQueue.Enqueue(bulkQueueMessage);
        }

        public int GetCount()
        {
            return concurrentQueue.Count;
        }
    }
}
