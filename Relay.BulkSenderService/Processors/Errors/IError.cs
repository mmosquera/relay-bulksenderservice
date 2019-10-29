using System;

namespace Relay.BulkSenderService.Processors.Errors
{
    public interface IError
    {
        DateTime Date { get; set; }
        string Message { get; set; }
        void Process();
    }
}
