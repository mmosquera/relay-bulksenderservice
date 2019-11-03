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
        public BasicPreProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration) { }

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

        protected virtual void DownloadAttachments(string fileName, IUserConfiguration userConfiguration)
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
                        if (index < fields.Length)
                        {
                            string attachmentFile = fields[index];

                            GetAttachmentFile(attachmentFile, fileName, userConfiguration);
                        }
                    }
                }
            }
        }

        protected void GetAttachmentFile(string attachmentFile, string originalFile, IUserConfiguration userConfiguration)
        {
            if (string.IsNullOrEmpty(attachmentFile))
            {
                return;
            }

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            string subFolder = Path.GetFileNameWithoutExtension(originalFile);

            string localAttachmentFolder = filePathHelper.GetAttachmentsFilesFolder(subFolder);

            if (!Directory.Exists(localAttachmentFolder))
            {
                Directory.CreateDirectory(localAttachmentFolder);
            }

            //local file 
            string localAttachmentFile = $@"{localAttachmentFolder}\{attachmentFile}";

            if (File.Exists(localAttachmentFile))
            {
                return;
            }

            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(originalFile);

            //get from ftp
            string ftpAttachmentFile = $@"{templateConfiguration.AttachmentsFolder}/{attachmentFile}";

            var ftpHelper = userConfiguration.Ftp.GetFtpHelper(_logger);
            ftpHelper.DownloadFile(ftpAttachmentFile, localAttachmentFile);

            if (File.Exists(localAttachmentFile))
            {
                ftpHelper.DeleteFile(ftpAttachmentFile);
                return;
            }

            //get from zip file
            string zipAttachments = $@"{templateConfiguration.AttachmentsFolder}/{Path.GetFileNameWithoutExtension(originalFile)}.zip";
            string localZipFile = $@"{filePathHelper.GetAttachmentsFilesFolder()}\{Path.GetFileNameWithoutExtension(originalFile)}.zip";

            // TODO: add retries.
            ftpHelper.DownloadFile(zipAttachments, localZipFile);

            if (File.Exists(localZipFile))
            {
                var zipHelper = new ZipHelper();
                zipHelper.UnzipFile(localZipFile, localAttachmentFolder);

                ftpHelper.DeleteFile(zipAttachments);
                File.Delete(localZipFile); //TODO add retries.
            }
        }
    }
}
