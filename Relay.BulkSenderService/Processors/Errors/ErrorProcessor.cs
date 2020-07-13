using Relay.BulkSenderService.Configuration;

namespace Relay.BulkSenderService.Processors.Errors
{
    public abstract class ErrorProcessor : IErrorProcessor
    {        
        protected readonly IConfiguration _configuration;

        public ErrorProcessor(IConfiguration configuration)
        {            
            _configuration = configuration;
        }

        public abstract void ProcessError(IError error);
    }
}
