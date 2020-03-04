using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.IO;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public abstract class ZipPreProcessor : PreProcessor
    {
        public ZipPreProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
        }

        public string GetUnzipFolder(string fileName, IUserConfiguration userConfiguration)
        {
            if (!Path.GetExtension(fileName).Equals(Constants.EXTENSION_ZIP, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            string name = Path.GetFileNameWithoutExtension(fileName);

            string downloadFolder = filePathHelper.GetDownloadsFolder();

            string unzipFolder = $@"{filePathHelper.GetAttachmentsFilesFolder()}\{name}";

            Directory.CreateDirectory(unzipFolder);

            var zipHelper = new ZipHelper();
            zipHelper.UnzipAll(fileName, unzipFolder);

            File.Delete(fileName);

            return unzipFolder;
        }
    }
}
