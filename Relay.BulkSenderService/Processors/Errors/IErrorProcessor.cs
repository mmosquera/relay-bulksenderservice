namespace Relay.BulkSenderService.Processors.Errors
{
    public interface IErrorProcessor
    {   
        void ProcessError(IError error);
    }
}
