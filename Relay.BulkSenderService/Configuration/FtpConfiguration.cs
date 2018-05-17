using Relay.BulkSenderService.Classes;

namespace Relay.BulkSenderService.Configuration
{
    public class FtpConfiguration : IFtpConfiguration
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public bool HasSSL { get; set; }

        public IFtpConfiguration Clone()
        {
            var ftpConfiguration = new FtpConfiguration();

            ftpConfiguration.Host = this.Host;
            ftpConfiguration.Username = this.Username;
            ftpConfiguration.Password = this.Password;
            ftpConfiguration.Port = this.Port;
            ftpConfiguration.HasSSL = this.HasSSL;

            return ftpConfiguration;
        }

        public IFtpHelper GetFtpHelper(ILog log)
        {
            var ftpHelper = new FTPHelper(log, this.Host, this.Port, this.Username, this.Password, this.HasSSL);

            return ftpHelper;
        }
    }
}
