using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public class GirePreProcessor : PreProcessor
    {
        public GirePreProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration) { }

        public override void ProcessFile(string fileName, IUserConfiguration userConfiguration)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            try
            {
                if (Path.GetExtension(fileName).Equals(Constants.EXTENSION_ZIP, StringComparison.OrdinalIgnoreCase))
                {
                    var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

                    string downloadPath = filePathHelper.GetDownloadsFolder();
                    string processedPath = filePathHelper.GetProcessedFilesFolder();

                    List<string> zipEntries = new ZipHelper().UnzipFile(fileName, downloadPath);

                    try
                    {
                        File.Delete(fileName); // Delete zip file                        
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Error trying to delete zip file -- {e}");
                    }

                    foreach (string zipEntry in zipEntries)
                    {
                        string name = Path.GetFileNameWithoutExtension(zipEntry);

                        if (File.Exists($@"{downloadPath}\{name}{Constants.EXTENSION_PROCESSING}") ||
                            File.Exists($@"{processedPath}\{name}{Constants.EXTENSION_PROCESSING}"))
                        {
                            _logger.Info($"The file {zipEntry} is processing.");

                            File.Delete(zipEntry);

                            continue;
                        }

                        if (File.Exists($@"{processedPath}\{name}{Constants.EXTENSION_PROCESSED}"))
                        {
                            _logger.Error($"The file {zipEntry} is already processed.");

                            File.Delete(zipEntry);

                            continue;
                        }

                        string newFileName = zipEntry.Replace(Path.GetExtension(zipEntry), Constants.EXTENSION_PROCESSING);

                        File.Move(zipEntry, newFileName);
                    }
                }
                else if (Path.GetExtension(fileName).Equals(".Ok", StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(fileName);
                }
                else
                {
                    string newFileName = fileName.Replace(Path.GetExtension(fileName), Constants.EXTENSION_PROCESSING);

                    File.Move(fileName, newFileName);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"ERROR GIRE PRE PROCESSOR: {e}");
            }
        }
    }
}
