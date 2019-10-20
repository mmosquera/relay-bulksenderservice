using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public class PreProcessWorker : BaseWorker
    {
        public PreProcessWorker(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {

        }

        public void Process()
        {
            while (true)
            {
                try
                {
                    CheckConfigChanges();

                    foreach (IUserConfiguration user in _users)
                    {
                        var filePathHelper = new FilePathHelper(_configuration, user.Name);

                        List<string> downloadFiles = Directory.GetFiles(filePathHelper.GetDownloadsFolder()).ToList();

                        string[] extensions = user.FileExtensions != null ? user.FileExtensions.ToArray() : new string[] { ".csv" };

                        List<string> filterFiles = downloadFiles.Where(x => extensions.Any(y => x.EndsWith(y, StringComparison.OrdinalIgnoreCase))).ToList();

                        PreProcessor preProcessor = user.GetPreProcessor(_logger, _configuration);

                        foreach (string filterFile in filterFiles)
                        {
                            preProcessor.ProcessFile(filterFile, user);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"GENERAL PREPROCESS ERROR: {ex}");
                }

                Thread.Sleep(_configuration.PreProcessorInterval);
            }
        }
    }
}
