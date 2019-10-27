using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System.Collections.Generic;
using System.IO;

namespace Relay.BulkSenderService.Processors.Status
{
    public abstract class StatusProcessor
    {
        protected readonly ILog _logger;
        protected readonly IConfiguration _configuration;
        protected const int TIME_WAIT_FILE = 1000;
        public StatusProcessor(ILog logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public abstract void ProcessStatusFile(IUserConfiguration userConfiguration, List<string> statusFiles);

        protected bool IsFileInUse(string fileName)
        {
            bool locked = false;

            try
            {
                FileStream fileStream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                fileStream.Close();
            }
            catch (IOException)
            {
                locked = true;
            }

            return locked;
        }
    }
}
