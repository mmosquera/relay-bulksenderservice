using Relay.BulkSenderService.Configuration;

namespace Relay.BulkSenderService.Processors.Errors
{
    public class UnexpectedError : Error
    {
        public UnexpectedError(IConfiguration configuration) : base(configuration)
        {
        }

        protected override string GetBody()
        {
            return "There is an unexpected error processing file {{filename}}";
        }
    }
}
