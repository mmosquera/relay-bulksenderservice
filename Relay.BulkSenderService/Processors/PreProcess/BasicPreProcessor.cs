using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public class BasicPreProcessor : PreProcessor
    {
        public BasicPreProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
        }

        public override void ProcessFile(string fileName, IUserConfiguration userConfiguration)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            DownloadAttachments(fileName, userConfiguration);

            string newFileName = fileName.Replace(Path.GetExtension(fileName), ".processing");

            try
            {
                File.Move(fileName, newFileName);
            }
            catch (Exception e)
            {
                _logger.Error($"ERROR BASIC PRE PROCESSOR: {e}");
            }
        }

        private void DownloadAttachments(string fileName, IUserConfiguration userConfiguration)
        {
            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(fileName);

            if (templateConfiguration == null || !templateConfiguration.Fields.Any(x => x.IsAttachment))
            {
                return;
            }

            List<int> indexes = templateConfiguration.Fields.Where(x => x.IsAttachment).Select(x => x.Position).ToList();

            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(fileStream))
            {
                if (templateConfiguration.HasHeaders)
                {
                    reader.ReadLine();
                }

                string line;
                string[] fields;

                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    fields = line.Split(templateConfiguration.FieldSeparator);

                    foreach (int index in indexes)
                    {
                        if (fields.Length < index)
                        {
                            continue;
                        }

                        string attachmentFile = fields[index];

                        GetAttachmentFile(attachmentFile, fileName, userConfiguration);
                    }
                }
            }
        }

        protected void GetAttachmentFile(string attachmentFile, string originalFile, IUserConfiguration userConfiguration)
        {
            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            //local file 
            string localAttachmentFolder = filePathHelper.GetAttachmentsFilesFolder();
            string localAttachmentFile = $@"{localAttachmentFolder}\{attachmentFile}";

            if (File.Exists(localAttachmentFile))
            {
                return;
            }

            //local file in subfolder
            string subFolder = Path.GetFileNameWithoutExtension(originalFile);
            string localAttachmentSubFile = $@"{localAttachmentFolder}\{subFolder}\{attachmentFile}";

            if (File.Exists(localAttachmentSubFile))
            {
                return;
            }

            string ftpAttachmentFile = $@"{userConfiguration.AttachmentsFolder}/{attachmentFile}";

            var ftpHelper = userConfiguration.Ftp.GetFtpHelper(_logger);
            ftpHelper.DownloadFile(ftpAttachmentFile, localAttachmentFile);

            if (File.Exists(localAttachmentFile))
            {
                ftpHelper.DeleteFile(ftpAttachmentFile);
                return;
            }

            string zipAttachments = $@"{userConfiguration.AttachmentsFolder}/{Path.GetFileNameWithoutExtension(originalFile)}.zip";
            string localZipAttachments = $@"{localAttachmentFolder}\{Path.GetFileNameWithoutExtension(originalFile)}.zip";

            // TODO: add retries.
            ftpHelper.DownloadFile(zipAttachments, localZipAttachments);

            if (File.Exists(localZipAttachments))
            {
                string newZipDirectory = $@"{localAttachmentFolder}\{subFolder}";
                Directory.CreateDirectory(newZipDirectory);

                var zipHelper = new ZipHelper();
                zipHelper.UnzipFile(localZipAttachments, newZipDirectory);

                ftpHelper.DeleteFile(zipAttachments);
                File.Delete(localZipAttachments); //TODO add retries.
            }            
        }
    }
}
