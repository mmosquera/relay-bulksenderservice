using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.IO;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public class ProvinciaPreProcessor : PreProcessor
    {
        public ProvinciaPreProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
        }

        public override void ProcessFile(string fileName, string userName)
        {
            if (!File.Exists(fileName) || !Path.GetExtension(fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var filePathHelper = new FilePathHelper(_configuration, userName);

            string name = Path.GetFileNameWithoutExtension(fileName);

            string downloadFolder = filePathHelper.GetDownloadsFolder();

            string unzipFolder = $@"{filePathHelper.GetAttachmentsFilesFolder()}\{name}";

            //string downloadPath = $@"{downloadFolder}\{name}";

            Directory.CreateDirectory(unzipFolder);

            var zipHelper = new ZipHelper();
            zipHelper.UnzipFile(fileName, unzipFolder);

            File.Delete(fileName);

            string processingFile = $@"{unzipFolder}\EnviosControl.txt";
            if (File.Exists(processingFile))
            {
                string newEnviosControlPath = $@"{downloadFolder}\{name}.processing";
                File.Move(processingFile, newEnviosControlPath);
            }
        }
    }
}
