using Relay.BulkSenderService.Classes;

namespace Relay.BulkSenderService.Configuration
{
    public interface IFtpConfiguration
    {
        string Host { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        int Port { get; set; }
        bool HasSSL { get; set; }

        IFtpHelper GetFtpHelper(ILog log);
    }
}
