namespace Relay.BulkSenderService.Processors.Acknowledgement
{
    public interface IAckProcessor
    {        
        void ProcessAckFile(string fileName);
    }
}
