namespace Relay.BulkSenderService.Processors.Errors
{
    public class DownloadError : Error
    {
        public DownloadError() : base()
        {
            _message = "There problems to download file.";
        }
    }
}
