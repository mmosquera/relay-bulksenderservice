﻿using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Relay.BulkSenderService.Processors
{
    public class FtpMonitor : BaseWorker
    {
        private Dictionary<string, DateTime> _nextRun;
        private List<string> _pausedUsers;
        private object _lockObject;

        public FtpMonitor(ILog logger, IConfiguration configuration, IWatcher watcher) : base(logger, configuration, watcher)
        {
            _nextRun = new Dictionary<string, DateTime>();
            _pausedUsers = new List<string>();
            _lockObject = new object();
            CreateUserFolders();
            ((FileCommandsWatcher)_watcher).StartProcessEvent += FtpMonitor_StartProcessEvent;
            ((FileCommandsWatcher)_watcher).StopProcessEvent += FtpMonitor_StopProcessEvent;
        }

        public void ReadFtpFiles()
        {
            while (true)
            {
                try
                {
                    if (CheckConfigChanges())
                    {
                        CreateUserFolders();
                    }

                    foreach (IUserConfiguration user in _users)
                    {
                        if (IsProcessPaused(user.Name))
                        {
                            _logger.Info($"The process is temporally paused for user {user.Name}");
                            continue;
                        }

                        if (!IsValidInterval(user.Name))
                        {
                            continue;
                        }

                        string[] extensions = user.FileExtensions != null ? user.FileExtensions.ToArray() : new string[] { ".csv" };

                        var ftpHelper = user.Ftp.GetFtpHelper(_logger);

                        foreach (string folder in user.DownloadFolders)
                        {
                            List<string> files = ftpHelper.GetFileList(folder, extensions);

                            DownloadUserFiles(folder, files, user, ftpHelper);
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

        private void FtpMonitor_StopProcessEvent(object sender, CommandsEventArgs e)
        {
            _logger.Debug($"Stop process event for user {e.User}");
            string key = e.User.ToUpper();
            lock (_lockObject)
            {
                if (!_pausedUsers.Contains(key))
                {
                    _pausedUsers.Add(key);
                }
            }
        }

        private void FtpMonitor_StartProcessEvent(object sender, CommandsEventArgs e)
        {
            _logger.Debug($"Start process event for user {e.User}");
            string key = e.User.ToUpper();
            lock (_lockObject)
            {
                if (_pausedUsers.Contains(key))
                {
                    _pausedUsers.Remove(key);
                }
            }
        }

        private bool IsProcessPaused(string user)
        {
            string key = user.ToUpper();
            lock (_lockObject)
            {
                return _pausedUsers.Contains(key);
            }
        }

        private void DownloadUserFiles(string folder, List<string> files, IUserConfiguration user, IFtpHelper ftpHelper)
        {
            if (files.Count == 0)
            {
                return;
            }

            Thread threadDownload = new Thread(new ThreadStart(() =>
            {
                string downloadFolder = new FilePathHelper(_configuration, user.Name).GetDownloadsFolder();

                foreach (string file in files)
                {
                    if (Directory.GetFiles(downloadFolder).Length >= 4)
                    {
                        break;
                    }

                    if (user.Ack != null && IsAckFile(file, user.Ack))
                    {
                        ProcessAckFile(folder, file, user, ftpHelper);
                        continue;
                    }

                    var result = new ProcessResult();
                    result.ErrorFileName = GetErrorsFileName(file, user);

                    string ftpFileName = $"{folder}/{file}";

                    if (GetFileFromFTP(ftpFileName, user, ftpHelper, result))
                    {
                        RemoveFileFromFtp(ftpFileName, user, ftpHelper);
                    }
                }
            }));
            threadDownload.Start();
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

        private void CreateUserFolders()
        {
            foreach (IUserConfiguration user in _users)
            {
                new FilePathHelper(_configuration, user.Name).CreateUserFolders();
            }
        }

        private bool IsAckFile(string fileName, AckConfiguration ackConfiguration)
        {
            return ackConfiguration.FileExtensions.Exists(x => x.Equals(Path.GetExtension(fileName)));
        }

        private void ProcessAckFile(string folderName, string fileName, IUserConfiguration user, IFtpHelper ftpHelper)
        {
            string ftpFileName = $@"{folderName}/{fileName}";
            string localFileName = $@"{new FilePathHelper(_configuration, user.Name).GetReportsFilesFolder()}\{fileName}";

            _logger.Debug($"Process ack file {ftpFileName}");

            ftpHelper.DownloadFile(ftpFileName, localFileName);

            if (File.Exists(localFileName))
            {
                if (user.HasDeleteFtp)
                {
                    ftpHelper.DeleteFile(ftpFileName);
                }

                try
                {
                    _logger.Debug($"Delete local file {fileName}");

                    File.Delete(localFileName);
                }
                catch (Exception e)
                {
                    _logger.Error($"Error trying to delete file {localFileName} -- {e}");
                }
            }
        }

        private bool GetFileFromFTP(string file, IUserConfiguration user, IFtpHelper ftpHelper, ProcessResult result)
        {
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            string localFileName;

            if (!ValidateFile(file, filePathHelper, result, out localFileName))
            {
                return false;
            }

            _logger.Debug($"Start to download {file} for user {user.Name}");

            bool downloadResult = ftpHelper.DownloadFile(file, localFileName);

            if (downloadResult && File.Exists(localFileName))
            {
                if (Path.GetExtension(file).Equals(".zip", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (UnzipFile(localFileName, filePathHelper.GetDownloadsFolder()) == 0)
                    {
                        string message = $"{DateTime.UtcNow}:Problems to unzip the file {file}.";
                        result.Type = ResulType.UNZIP;
                        result.WriteError(message);
                        return false;
                    }

                    try
                    {
                        File.Delete(localFileName); // Delete zip file                        
                    }
                    catch { }
                }
                else
                {
                    string newFileName = localFileName.Replace(".downloading", ".processing");

                    File.Move(localFileName, newFileName);
                }
            }
            else
            {
                string message = $"{DateTime.UtcNow}:Problems to download the file {file}.";
                result.Type = ResulType.DOWNLOAD;
                result.WriteError(message);
                _logger.Error($"Download problems with file {file}.");

                try
                {
                    // Delete wrong file to retry.
                    File.Delete(localFileName);
                }
                catch { }

                return false;
            }

            return true;
        }

        private bool ValidateFile(string file, FilePathHelper filePathHelper, ProcessResult result, out string localFileName)
        {
            string fileName = Path.GetFileName(file);
            string name = Path.GetFileNameWithoutExtension(fileName);

            string downloadPath = filePathHelper.GetDownloadsFolder();
            string processedPath = filePathHelper.GetProcessedFilesFolder();

            localFileName = $@"{downloadPath}\{name}.downloading";

            if (File.Exists(localFileName))
            {
                _logger.Info($"The file {fileName} is downloading.");
                return false;
            }

            if (File.Exists($@"{downloadPath}\{name}.processing") || File.Exists($@"{processedPath}\{name}.processing"))
            {
                _logger.Info($"The file {fileName} is processing.");
                return false;
            }

            if (File.Exists($@"{processedPath}\{name}.processed"))
            {
                string message = $"{DateTime.UtcNow}:The file {Path.GetFileName(fileName)} is already processed.";
                _logger.Error($"The file {fileName} is already processed.");
                result.Type = ResulType.REPEATED;
                result.WriteError(message);
                return false;
            }

            return true;
        }

        private int UnzipFile(string fileName, string path)
        {
            int count = 0;
            string newFileName = null;

            try
            {
                using (ZipArchive zipArchive = ZipFile.OpenRead(fileName))
                {
                    foreach (ZipArchiveEntry entry in zipArchive.Entries)
                    {
                        newFileName = $@"{path}\{Path.GetFileNameWithoutExtension(entry.FullName)}.processing";

                        entry.ExtractToFile(newFileName, true);

                        count++;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error trying to unzip file -- {e}");
                return count;
            }

            return count;
        }

        private void RemoveFileFromFtp(string file, IUserConfiguration user, IFtpHelper ftpHelper)
        {
            if (user.HasDeleteFtp)
            {
                _logger.Debug($"Remove file {file} from FTP ");

                ftpHelper.DeleteFile(file);
            }
        }

        private string GetErrorsFileName(string file, IUserConfiguration user)
        {
            string errorsFilePath = null;

            if (user.Errors != null)
            {
                var filePathHelper = new FilePathHelper(_configuration, user.Name);

                string errorsPath = filePathHelper.GetResultsFilesFolder();

                errorsFilePath = $@"{errorsPath}\{user.Errors.Name.GetReportName(file)}";
            }

            return errorsFilePath;
        }
    }
}
