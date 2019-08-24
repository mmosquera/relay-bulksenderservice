using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Relay.BulkSenderService.Processors
{
    public class CleanProcessor : BaseWorker
    {
        public CleanProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
        }

        public void Process()
        {
            while (true)
            {
                try
                {
                    CheckConfigChanges();

                    _logger.Debug($"Start to delete local files.");

                    foreach (IUserConfiguration user in _users)
                    {
                        DeleteAttachmentsFiles(user);

                        DeleteUserFiles(user);
                    }

                    DeleteReportsFiles();
                }
                catch (Exception e)
                {
                    _logger.Error($"General error on clean process -- {e}");
                }

                Thread.Sleep(_configuration.CleanInterval);
            }
        }

        private void DeleteAttachmentsFiles(IUserConfiguration user)
        {
            string attachmentsFolder = new FilePathHelper(_configuration, user.Name).GetAttachmentsFilesFolder();

            DateTime filterDate = DateTime.UtcNow.AddDays(-_configuration.CleanAttachmentsDays);

            DeleteFilesRecursively(attachmentsFolder, filterDate);

            DirectoryInfo directory = new DirectoryInfo(attachmentsFolder);

            if (directory.Exists)
            {
                DirectoryInfo[] directories = directory.GetDirectories();

                foreach (DirectoryInfo subDirectory in directories)
                {
                    DeleteFolder(subDirectory, filterDate);
                }
            }
        }

        private void DeleteFolder(DirectoryInfo folder, DateTime dateFilter)
        {
            if (folder.Exists && folder.GetFiles().Length == 0 && folder.CreationTimeUtc < dateFilter)
            {
                try
                {
                    folder.Delete();
                }
                catch (Exception e)
                {
                    _logger.Error($"Error trying to delete folder:{folder.FullName} -- {e}");
                }
            }
        }

        private void DeleteUserFiles(IUserConfiguration userConfiguration)
        {
            string folder = new FilePathHelper(_configuration, userConfiguration.Name).GetUserFolder();

            DateTime filterDate = DateTime.UtcNow.AddDays(-_configuration.CleanDays);

            DeleteFilesRecursively(folder, filterDate);
        }

        private void DeleteFilesRecursively(string folder, DateTime filter)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            var directoryInfo = new DirectoryInfo(folder);
            foreach (DirectoryInfo subDirectory in directoryInfo.GetDirectories())
            {
                DeleteFilesRecursively(subDirectory.FullName, filter);
            }

            FileInfo[] files = directoryInfo.GetFiles().Where(f => f.CreationTimeUtc < filter).ToArray();

            foreach (FileInfo file in files)
            {
                try
                {
                    file.Delete();
                }
                catch (Exception e)
                {
                    _logger.Error($"Error trying to delete file:{file.FullName} -- {e}");
                }
            }
        }

        private void DeleteReportsFiles()
        {
            DateTime filterDate = DateTime.UtcNow.AddDays(-_configuration.CleanDays);

            DeleteFilesRecursively(_configuration.ReportsFolder, filterDate);
        }
    }
}
