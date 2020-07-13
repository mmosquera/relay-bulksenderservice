namespace Relay.BulkSenderService.Processors.Errors
{
    public class UnexpectedError : Error
    {
        public UnexpectedError() : base()
        {
            _message = "There is an unexpected error processing file. Please check the application log.";
        }
    }
}
