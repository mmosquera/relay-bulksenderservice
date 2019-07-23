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
        private Dictionary<string, int> _threadsCount;
        private object _lockObj;

        public LocalMonitor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
            _threadsCount = new Dictionary<string, int>();
            _lockObj = new object();
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

        private bool ProcessFile(string fileName, IUserConfiguration user, FilePathHelper filePathHelper)
        {
            int threadsUserCount = GetThreadCount(user.Name);

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

            IncrementUserThreadCount(user.Name);
            ThreadPool.QueueUserWorkItem(new WaitCallback(processor.DoWork), threadState);

            // To mode debug.
            //processor.DoWork(threadState);

            return true;
        }

        private void ProcessFinishedHandler(object sender, ThreadEventArgs args)
        {
            _logger.Debug($"Finish to process ThreadId:{Thread.CurrentThread.ManagedThreadId} for user:{args.Name}");

            DecrementUserThreadCount(args.Name);
        }

        private void IncrementUserThreadCount(string user)
        {
            string key = user.ToUpper();
            lock (_lockObj)
            {
                if (_threadsCount.ContainsKey(key))
                {
                    _threadsCount[key]++;
                }
                else
                {
                    _threadsCount.Add(key, 1);
                }
            }
        }

        private void DecrementUserThreadCount(string user)
        {
            string key = user.ToUpper();
            lock (_lockObj)
            {
                if (_threadsCount.ContainsKey(key))
                {
                    _threadsCount[key]--;
                }
            }
        }

        private int GetThreadCount(string user)
        {
            string key = user.ToUpper();
            lock (_lockObj)
            {
                if (_threadsCount.ContainsKey(key))
                {
                    return _threadsCount[key];
                }
            }
            return 0;
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
    }
}
