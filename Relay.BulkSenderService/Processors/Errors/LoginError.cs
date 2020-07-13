namespace Relay.BulkSenderService.Processors.Errors
{
    public class LoginError : Error
    {
        public LoginError() : base()
        {
            _message = $"There are problems to authenticate user with DopplerRelay.";
        }
    }
}
