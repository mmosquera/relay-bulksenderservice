namespace Relay.BulkSenderService.Processors.Errors
{
    public class FileRepeatedError : Error
    {
        public FileRepeatedError() : base()
        {
            _message = $"The file is repeated";
        }
    }
}
