using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Processors.Acknowledgement;
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
        private const int MINUTES_TO_WRITE_DOWNLOAD = 5;
        private const int MINUTES_TO_DELETE_REPEATED = 60;
        private Dictionary<string, DateTime> _nextRun;
        private Dictionary<string, List<RepeatedFile>> _repeatedFiles;
        private readonly object _lockRepeatedFiles;

        public FtpMonitor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
            _nextRun = new Dictionary<string, DateTime>();
            _repeatedFiles = new Dictionary<string, List<RepeatedFile>>();
            _lockRepeatedFiles = new object();
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

                        List<string> downloadFolders = user.Templates.SelectMany(x => x.DownloadFolders).Distinct().ToList();

                        ProcessAckFiles(user, downloadFolders);

                        IEnumerable<string> extensions = user.FileExtensions != null ? user.FileExtensions : new List<string> { ".csv" };

                        var ftpHelper = user.Ftp.GetFtpHelper(_logger);

                        foreach (string folder in downloadFolders)
                        {
                            List<string> files = ftpHelper.GetFileList(folder, extensions);

                            Task.Factory.StartNew(() => DownloadUserFiles(folder, files, user));
                        }

                        RemoveRepeatedFiles(user);

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

        private void ProcessAckFiles(IUserConfiguration userConfiguration, List<string> folders)
        {
            if (userConfiguration.Ack == null)
            {
                return;
            }

            var ftpHelper = userConfiguration.Ftp.GetFtpHelper(_logger);

            IAckProcessor ackProcessor = userConfiguration.GetAckProcessor(_logger, _configuration);

            foreach (string folder in folders)
            {
                List<string> ackFiles = ftpHelper.GetFileList(folder, userConfiguration.Ack.Extensions);

                foreach (string ackFile in ackFiles)
                {
                    string ftpFileName = $"{folder}/{ackFile}";
                    string localFileName = $@"{new FilePathHelper(_configuration, userConfiguration.Name).GetDownloadsFolder()}\{ackFile}";

                    if (ftpHelper.DownloadFileWithResume(ftpFileName, localFileName))
                    {
                        ackProcessor.ProcessAckFile(localFileName);

                        RemoveFileFromFtp(ftpFileName, userConfiguration);
                    }
                }
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
                string newFileName = $@"{new FilePathHelper(_configuration, user.Name).GetDownloadsFolder()}\{Path.GetFileName(file)}";

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
            string retryPath = filePathHelper.GetRetriesFilesFolder();

            localFileName = $@"{downloadPath}\{name}{Constants.EXTENSION_DOWNLOADING}";

            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(file);

            bool allowDuplicates = templateConfiguration != null && templateConfiguration.AllowDuplicates ? true : false;

            var fileInfo = new FileInfo(localFileName);

            if (fileInfo.Exists && DateTime.UtcNow.Subtract(fileInfo.LastWriteTimeUtc).TotalMinutes < MINUTES_TO_WRITE_DOWNLOAD)
            {
                _logger.Info($"The file {fileName} is downloading.");
                return false;
            }

            if (!allowDuplicates &&
                (File.Exists($@"{downloadPath}\{name}{Constants.EXTENSION_PROCESSING}") ||
                File.Exists($@"{processedPath}\{name}{Constants.EXTENSION_PROCESSING}") ||
                File.Exists($@"{processedPath}\{name}{Constants.EXTENSION_PROCESSED}") ||
                File.Exists($@"{retryPath}\{name}{Constants.EXTENSION_PROCESSING}") ||
                File.Exists($@"{retryPath}\{name}{Constants.EXTENSION_RETRY}")))
            {
                _logger.Error($"The file {fileName} is already processed.");

                if (AddRepeatedFile(userConfiguration, file))
                {
                    new FileRepeatedError(_configuration).SendErrorEmail(fileName, userConfiguration.Alerts);
                }

                return false;
            }

            return true;
        }

        private bool AddRepeatedFile(IUserConfiguration userConfiguration, string fileName)
        {
            lock (_lockRepeatedFiles)
            {
                if (!_repeatedFiles.ContainsKey(userConfiguration.Name))
                {
                    _repeatedFiles.Add(userConfiguration.Name, new List<RepeatedFile>());
                }

                if (!_repeatedFiles[userConfiguration.Name].Any(x => x.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _repeatedFiles[userConfiguration.Name].Add(new RepeatedFile()
                    {
                        Name = fileName,
                        RepeatedTime = DateTime.UtcNow
                    });

                    return true;
                }
            }

            return false;
        }

        private void RemoveRepeatedFiles(IUserConfiguration userConfiguration)
        {
            lock (_lockRepeatedFiles)
            {
                if (_repeatedFiles.ContainsKey(userConfiguration.Name))
                {
                    IEnumerable<RepeatedFile> filesToDelete = _repeatedFiles[userConfiguration.Name].Where(x => DateTime.UtcNow.Subtract(x.RepeatedTime).TotalMinutes > MINUTES_TO_DELETE_REPEATED);

                    foreach (RepeatedFile file in filesToDelete)
                    {
                        RemoveFileFromFtp(file.Name, userConfiguration);
                    }

                    _repeatedFiles[userConfiguration.Name].RemoveAll(x => DateTime.UtcNow.Subtract(x.RepeatedTime).TotalMinutes > MINUTES_TO_DELETE_REPEATED);

                    if (_repeatedFiles[userConfiguration.Name].Count == 0)
                    {
                        _repeatedFiles.Remove(userConfiguration.Name);
                    }
                }
            }
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
