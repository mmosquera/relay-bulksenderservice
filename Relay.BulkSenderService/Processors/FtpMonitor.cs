using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Processors.Errors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.BulkSenderService.Processors
{
    public class FtpMonitor : BaseWorker
    {
        private const int MINUTES_TO_WRITE = 5;
        private Dictionary<string, DateTime> _nextRun;

        public FtpMonitor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
            _nextRun = new Dictionary<string, DateTime>();
        }

        public void ReadFtpFiles()
        {
            while (true)
            {
                try
                {
                    CheckConfigChanges();

                    foreach (IUserConfiguration user in _users)
                    {
                        if (!IsValidInterval(user.Name))
                        {
                            continue;
                        }

                        string[] extensions = user.FileExtensions != null ? user.FileExtensions.ToArray() : new string[] { ".csv" };

                        var ftpHelper = user.Ftp.GetFtpHelper(_logger);

                        List<string> downloadFolders = user.Templates.SelectMany(x => x.DownloadFolders).Distinct().ToList();

                        foreach (string folder in downloadFolders)
                        {
                            List<string> files = ftpHelper.GetFileList(folder, extensions);

                            Task.Factory.StartNew(() => DownloadUserFiles(folder, files, user));
                        }

                        SetNextRun(user.Name, user.FtpInterval);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"GENERAL FTP MONITOR ERROR: {ex}");
                }

                Thread.Sleep(_configuration.FtpListInterval);
            }
        }

        private void DownloadUserFiles(string folder, List<string> files, IUserConfiguration user)
        {
            if (files.Count == 0)
            {
                return;
            }

            int parallelProcessors = user.MaxParallelProcessors != 0 ? user.MaxParallelProcessors : _configuration.MaxNumberOfThreads;

            int totalFiles = parallelProcessors * 2;

            string downloadFolder = new FilePathHelper(_configuration, user.Name).GetDownloadsFolder();

            foreach (string file in files)
            {
                if (Directory.GetFiles(downloadFolder).Length >= totalFiles)
                {
                    break;
                }

                string ftpFileName = $"{folder}/{file}";

                if (GetFileFromFTP(ftpFileName, user))
                {
                    RemoveFileFromFtp(ftpFileName, user);
                }
            }
        }

        private void SetNextRun(string name, int ftpInterval)
        {
            if (ftpInterval > 0 && ftpInterval > (_configuration.FtpListInterval / 60000))
            {
                if (_nextRun.ContainsKey(name))
                {
                    _nextRun[name] = DateTime.UtcNow.AddMinutes(ftpInterval);
                }
                else
                {
                    _nextRun.Add(name, DateTime.UtcNow.AddMinutes(ftpInterval));
                }
            }
            else
            {
                if (_nextRun.ContainsKey(name))
                {
                    _nextRun.Remove(name);
                }
            }
        }

        private bool IsValidInterval(string name)
        {
            if (_nextRun.ContainsKey(name) && _nextRun[name] > DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }

        private bool GetFileFromFTP(string file, IUserConfiguration user)
        {
            string localFileName;

            if (!ValidateFile(file, user, out localFileName))
            {
                return false;
            }

            _logger.Debug($"Start to download {file} for user {user.Name}");

            IFtpHelper ftpHelper = user.Ftp.GetFtpHelper(_logger);

            bool downloadResult = ftpHelper.DownloadFileWithResume(file, localFileName);

            if (downloadResult && File.Exists(localFileName))
            {
                string newFileName = localFileName.Replace(Constants.EXTENSION_DOWNLOADING, Path.GetExtension(file));
                File.Move(localFileName, newFileName);
            }
            else
            {
                new DownloadError(_configuration).SendErrorEmail(file, user.Alerts);

                _logger.Error($"Download problems with file {file}.");

                return false;
            }

            return true;
        }

        private bool ValidateFile(string file, IUserConfiguration userConfiguration, out string localFileName)
        {
            string fileName = Path.GetFileName(file);
            string name = Path.GetFileNameWithoutExtension(fileName);

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            string downloadPath = filePathHelper.GetDownloadsFolder();
            string processedPath = filePathHelper.GetProcessedFilesFolder();

            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(file);

            bool allowDuplicates = templateConfiguration != null && templateConfiguration.AllowDuplicates ? true : false;

            if (allowDuplicates)
            {
                var directory = new DirectoryInfo(processedPath);
                int count = 1;

                string auxName = $@"{name}_{count.ToString("000")}";

                while (directory.GetFiles().ToList().Any(x => x.Name.Contains(auxName)))
                {
                    count++;
                    auxName = $@"{name}_{count.ToString("000")}";
                }

                localFileName = $@"{downloadPath}\{name}_{count.ToString("000")}{Constants.EXTENSION_DOWNLOADING}";
            }
            else
            {
                localFileName = $@"{downloadPath}\{name}{Constants.EXTENSION_DOWNLOADING}";
            }

            var fileInfo = new FileInfo(localFileName);

            if (fileInfo.Exists && DateTime.UtcNow.Subtract(fileInfo.LastWriteTimeUtc).TotalMinutes < MINUTES_TO_WRITE)
            {
                _logger.Info($"The file {fileName} is downloading.");
                return false;
            }

            if (File.Exists($@"{downloadPath}\{name}{Constants.EXTENSION_PROCESSING}") ||
                File.Exists($@"{processedPath}\{name}{Constants.EXTENSION_PROCESSING}") ||
                File.Exists($@"{processedPath}\{name}{Constants.EXTENSION_PROCESSED}"))
            {
                _logger.Error($"The file {fileName} is already processed.");

                new FileRepeatedError(_configuration).SendErrorEmail(fileName, userConfiguration.Alerts);

                RemoveFileFromFtp(file, userConfiguration);

                return false;
            }

            return true;
        }

        private void RemoveFileFromFtp(string file, IUserConfiguration user)
        {
            if (user.HasDeleteFtp)
            {
                _logger.Debug($"Remove file {file} from FTP ");

                IFtpHelper ftpHelper = user.Ftp.GetFtpHelper(_logger);

                ftpHelper.DeleteFile(file);
            }
        }
    }
}
