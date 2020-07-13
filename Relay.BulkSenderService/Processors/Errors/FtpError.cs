namespace Relay.BulkSenderService.Processors.Errors
{
    public class FtpError : Error
    {
        public FtpError() : base()
        {
            _message = "There are problems with FTP Connection. Please check the application log.";
        }
    }
}
