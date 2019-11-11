using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Relay.BulkSenderService.Processors
{
    public class LocalMonitor : BaseWorker
    {
        private const int MINUTES_TO_CHECK = 60;
        private const int MINUTES_TO_RETRY = 5;
        private Dictionary<string, List<ProcessingFile>> _processingFiles;
        private object _lockProcessingFiles;

        public LocalMonitor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
            _processingFiles = new Dictionary<string, List<ProcessingFile>>();
            _lockProcessingFiles = new object();
        }

        public void ReadLocalFiles()
        {
            while (true)
            {
                try
                {
                    CheckConfigChanges();

                    foreach (IUserConfiguration user in _users)
                    {
                        var filePathHelper = new FilePathHelper(_configuration, user.Name);

                        ReprocessFailedFiles(user);

                        List<string> retryFiles = Directory.GetFiles(filePathHelper.GetRetriesFilesFolder(), "*.retry").ToList();

                        foreach (string retryFile in retryFiles)
                        {
                            if (!ProcessFile(retryFile, user, filePathHelper))
                            {
                                break;
                            }
                        }

                        List<string> downloadFiles = Directory.GetFiles(filePathHelper.GetDownloadsFolder(), "*.processing").ToList();

                        foreach (string downloadFile in downloadFiles)
                        {
                            if (!ProcessFile(downloadFile, user, filePathHelper))
                            {
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"GENERAL LOCAL MONITOR ERROR: {ex}");
                }

                Thread.Sleep(_configuration.LocalFilesInterval);
            }
        }

        private void ReprocessFailedFiles(IUserConfiguration user)
        {
            UpdateProcessingFiles(user);

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            List<string> retryFiles = Directory.GetFiles(filePathHelper.GetRetriesFilesFolder(), "*.processing").ToList();

            foreach (string file in retryFiles)
            {
                if (!IsFileProcessing(user.Name, file))
                {
                    string newRetryFile = $@"{filePathHelper.GetRetriesFilesFolder()}\{Path.GetFileNameWithoutExtension(file)}.retry";

                    File.Move(file, newRetryFile);
                }
            }

            List<string> processFiles = Directory.GetFiles(filePathHelper.GetProcessedFilesFolder(), "*.processing").ToList();

            foreach (string file in processFiles)
            {
                if (!IsFileProcessing(user.Name, file))
                {
                    string newRetryFile = $@"{filePathHelper.GetRetriesFilesFolder()}\{Path.GetFileNameWithoutExtension(file)}.retry";

                    File.Move(file, newRetryFile);
                }
            }
        }

        private void UpdateProcessingFiles(IUserConfiguration user)
        {
            List<string> processingFiles = null;

            lock (_lockProcessingFiles)
            {
                if (_processingFiles.ContainsKey(user.Name))
                {
                    processingFiles = _processingFiles[user.Name].Where(x => DateTime.UtcNow.Subtract(x.LastUpdate).TotalMinutes > MINUTES_TO_CHECK).Select(x => x.FileName).ToList();
                }
            }

            if (processingFiles == null || processingFiles.Count == 0)
            {
                return;
            }

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var queueDirectory = new DirectoryInfo(filePathHelper.GetQueueFilesFolder());

            foreach (string file in processingFiles)
            {
                List<FileInfo> queueFiles = queueDirectory.GetFiles($"{file}.*").ToList();

                if (queueFiles.Any(x => DateTime.UtcNow.Subtract(x.LastWriteTimeUtc).TotalMinutes < MINUTES_TO_RETRY))
                {
                    lock (_lockProcessingFiles)
                    {
                        _processingFiles[user.Name].FirstOrDefault(x => x.FileName == file).LastUpdate = DateTime.UtcNow;
                    }
                }
                else
                {
                    //TODO enviar alerta
                    //aca llego porque se rompio un archivo pero no deberia pasar nunca.
                    string processedFile = $@"{filePathHelper.GetProcessedFilesFolder()}\{file}.processing";

                    if (File.Exists(processedFile))
                    {
                        string corruptedFile = $@"{filePathHelper.GetProcessedFilesFolder()}\{file}.corrupted";
                        File.Move(processedFile, corruptedFile);
                    }

                    RemoveProcessingFile(user.Name, file);
                }
            }
        }

        private bool ProcessFile(string fileName, IUserConfiguration user, FilePathHelper filePathHelper)
        {
            int threadsUserCount = GetProcessorsCount(user.Name);

            int parallelProcessors = user.MaxParallelProcessors != 0 ? user.MaxParallelProcessors : _configuration.MaxNumberOfThreads;

            if (threadsUserCount >= parallelProcessors)
            {
                _logger.Debug($"There is no thread available for user {user.Name}. Is working with {threadsUserCount} threads.");
                return false;
            }

            var processor = user.GetProcessor(_logger, _configuration, fileName);

            if (processor == null)
            {
                _logger.Error($"Error to process file:{fileName}. Can't find processor for file.");
                return true;
            }

            string destFileName;

            if (!fileName.EndsWith(".retry"))
            {
                destFileName = $@"{filePathHelper.GetProcessedFilesFolder()}\{Path.GetFileName(fileName)}";
                File.Move(fileName, destFileName);
            }
            else
            {
                destFileName = fileName.Replace(".retry", ".processing");
                File.Move(fileName, destFileName);
            }

            _logger.Debug($"New thread for user:{user.Name}. Thread count:{threadsUserCount + 1}");

            var threadState = new ThreadStateInfo
            {
                FileName = destFileName,
                User = user,
                Handler = new EventHandler<ThreadEventArgs>(ProcessFinishedHandler)
            };

            AddProcessingFile(user.Name, destFileName);

            ThreadPool.QueueUserWorkItem(new WaitCallback(processor.DoWork), threadState);

            // To mode debug.
            //processor.DoWork(threadState);

            return true;
        }

        private void ProcessFinishedHandler(object sender, ThreadEventArgs args)
        {
            _logger.Debug($"Finish to process ThreadId:{Thread.CurrentThread.ManagedThreadId} for user:{args.Name}");

            RemoveProcessingFile(args.Name, args.FileName);
        }

        private int GetProcessorsCount(string userName)
        {
            lock (_lockProcessingFiles)
            {
                if (_processingFiles.ContainsKey(userName))
                {
                    return _processingFiles[userName].Count;
                }

                return 0;
            }
        }

        private void AddProcessingFile(string userName, string fileName)
        {
            lock (_lockProcessingFiles)
            {
                if (!_processingFiles.ContainsKey(userName))
                {
                    var list = new List<ProcessingFile>();
                    _processingFiles.Add(userName, list);
                }

                var processingFile = new ProcessingFile()
                {
                    FileName = Path.GetFileNameWithoutExtension(fileName),
                    LastUpdate = DateTime.UtcNow
                };

                _processingFiles[userName].Add(processingFile);
            }
        }

        private void RemoveProcessingFile(string userName, string fileName)
        {
            lock (_processingFiles)
            {
                if (_processingFiles.ContainsKey(userName))
                {
                    _processingFiles[userName].RemoveAll(x => x.FileName == Path.GetFileNameWithoutExtension(fileName));
                }
            }
        }

        private bool IsFileProcessing(string userName, string fileName)
        {
            lock (_processingFiles)
            {
                if (_processingFiles.ContainsKey(userName))
                {
                    return _processingFiles[userName].Any(x => x.FileName == Path.GetFileNameWithoutExtension(fileName));
                }

                return false;
            }
        }
    }

    public class ThreadStateInfo
    {
        public IUserConfiguration User { get; set; }
        public string FileName { get; set; }
        public EventHandler<ThreadEventArgs> Handler { get; set; }
    }

    public class ThreadEventArgs : EventArgs
    {
        public string Name { get; set; }
        public string FileName { get; set; }
    }

    public class FileStatus
    {
        public string FileName { get; set; }
        public int Total { get; set; }
        public int Processed { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool Finished { get; set; }
    }
}
