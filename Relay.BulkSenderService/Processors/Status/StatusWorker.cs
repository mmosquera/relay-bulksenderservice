using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Relay.BulkSenderService.Processors.Status
{
    public class StatusWorker : BaseWorker
    {
        public StatusWorker(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {

        }

        public void Process()
        {
            while (true)
            {
                try
                {
                    foreach (IUserConfiguration user in _users.Where(x => x.Status != null))
                    {
                        var filePathHelper = new FilePathHelper(_configuration, user.Name);

                        StatusProcessor statusProcessor = user.GetStatusProcessor(_logger, _configuration);

                        var directoryInfo = new DirectoryInfo(filePathHelper.GetQueueFilesFolder());

                        statusProcessor.ProcessStatusFile(user, directoryInfo.GetFiles("*.status.tmp").Select(x => x.FullName).ToList());
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"GENERAL STATUS PROCESSOR ERROR: {ex}");
                }

                Thread.Sleep(_configuration.StatusProcessorInterval);
            }
        }
    }
}
