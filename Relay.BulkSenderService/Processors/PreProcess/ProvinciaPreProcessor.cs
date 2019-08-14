using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.IO;
using System.Text;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public class ProvinciaPreProcessor : PreProcessor
    {
        private const int LINESXFILE = 50000;
        private const string SPLIT = "split"; //TODO: get from user configuration for split processor.

        public ProvinciaPreProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
        }

        public override void ProcessFile(string fileName, string userName)
        {
            if (!File.Exists(fileName) || !Path.GetExtension(fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {

                var filePathHelper = new FilePathHelper(_configuration, userName);

                string name = Path.GetFileNameWithoutExtension(fileName);

                string downloadFolder = filePathHelper.GetDownloadsFolder();

                string unzipFolder = $@"{filePathHelper.GetAttachmentsFilesFolder()}\{name}";

                Directory.CreateDirectory(unzipFolder);

                var zipHelper = new ZipHelper();
                zipHelper.UnzipAll(fileName, unzipFolder);

                File.Delete(fileName);

                string baproFile = $@"{unzipFolder}\EnviosControl.txt";
                if (File.Exists(baproFile))
                {
                    int totalLines = 0;
                    int index = 1;
                    string processingFile = $@"{downloadFolder}\{Path.GetFileNameWithoutExtension(fileName)}.{SPLIT}{index.ToString("00")}.processing";

                    var stringBuilder = new StringBuilder();

                    using (var streamReader = new StreamReader(baproFile))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            stringBuilder.AppendLine(line);

                            totalLines++;

                            if (totalLines >= LINESXFILE)
                            {
                                totalLines = 0;
                                index++;

                                using (var streamWriter = new StreamWriter(processingFile))
                                {
                                    streamWriter.Write(stringBuilder.ToString());
                                }

                                processingFile = $@"{downloadFolder}\{Path.GetFileNameWithoutExtension(fileName)}.{SPLIT}{index.ToString("00")}.processing";

                                stringBuilder.Clear();
                            }
                        }
                    }

                    if (stringBuilder.Length > 0)
                    {
                        using (var streamWriter = new StreamWriter(processingFile))
                        {
                            streamWriter.Write(stringBuilder.ToString());
                        }
                    }
                }
            }
            catch(Exception e)
            {
                _logger.Error($"ERROR PROVINCIA PRE PROCESSOR: {e}");
            }
        }
    }
}
