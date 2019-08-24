using Newtonsoft.Json;
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
        private Dictionary<string, object> _lockFiles;
        private object _lockFileObj;

        public LocalMonitor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
            _threadsCount = new Dictionary<string, int>();
            _lockObj = new object();
            _lockFiles = new Dictionary<string, object>();
            _lockFileObj = new object();
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
                        CleanStatusFile(user);

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

        private void CleanStatusFile(IUserConfiguration user)
        {
            if (user.Status == null)
            {
                return;
            }

            object locker;
            lock (_lockFileObj)
            {
                if (_lockFiles.ContainsKey(user.Name))
                {
                    locker = _lockFiles[user.Name];
                }
                else
                {
                    locker = new object();
                    _lockFiles.Add(user.Name, locker);
                }
            }

            lock (locker)
            {
                string userFolder = new FilePathHelper(_configuration, user.Name).GetUserFolder();
                string file = $"status.{user.Name}.json";

                string fileName = $@"{userFolder}\{file}";

                UserFilesStatus userFilesStatus;
                string jsonContent;

                if (File.Exists(fileName))
                {
                    using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var streamReader = new StreamReader(fileStream))
                    {
                        jsonContent = streamReader.ReadToEnd();
                    }

                    userFilesStatus = JsonConvert.DeserializeObject<UserFilesStatus>(jsonContent);

                    int count = userFilesStatus.Files.Count;

                    if (count == 0)
                    {
                        return;
                    }

                    userFilesStatus.Files.RemoveAll(x => x.Finished && DateTime.UtcNow.Subtract(x.LastUpdate).TotalHours > user.Status.LastViewingHours);

                    if (count == userFilesStatus.Files.Count)
                    {
                        return;
                    }

                    jsonContent = JsonConvert.SerializeObject(userFilesStatus);

                    using (var fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                    {
                        fileStream.SetLength(0);
                        using (var streamWriter = new StreamWriter(fileStream))
                        {
                            streamWriter.WriteLine(jsonContent);
                        }
                    }
                }
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
                Handler = new EventHandler<ThreadEventArgs>(ProcessFinishedHandler),
                StatusEventHandler = new EventHandler<StatusEventArgs>(StatusEventHandler)
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

        private void StatusEventHandler(object sender, StatusEventArgs args)
        {
            try
            {

                object locker;
                lock (_lockFileObj)
                {
                    if (_lockFiles.ContainsKey(args.UserName))
                    {
                        locker = _lockFiles[args.UserName];
                    }
                    else
                    {
                        locker = new object();
                        _lockFiles.Add(args.UserName, locker);
                    }
                }

                lock (locker)
                {
                    string userFolder = new FilePathHelper(_configuration, args.UserName).GetUserFolder();
                    string file = $"status.{args.UserName}.json";

                    string fileName = $@"{userFolder}\{file}";

                    UserFilesStatus userFilesStatus;
                    string jsonContent;

                    if (File.Exists(fileName))
                    {
                        using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (var streamReader = new StreamReader(fileStream))
                        {
                            jsonContent = streamReader.ReadToEnd();
                        }

                        userFilesStatus = JsonConvert.DeserializeObject<UserFilesStatus>(jsonContent);

                        FileStatus fileStatus = userFilesStatus.Files.FirstOrDefault(x => x.FileName == args.Status.FileName);

                        if (fileStatus != null)
                        {
                            fileStatus.Processed = args.Status.GetProcessedCount();
                            fileStatus.Finished = args.Status.Finished;
                            fileStatus.LastUpdate = DateTime.UtcNow;
                        }
                        else
                        {
                            userFilesStatus.Files.Add(new FileStatus()
                            {
                                FileName = args.Status.FileName,
                                Total = args.Status.GetTotalCount(),
                                Processed = args.Status.GetProcessedCount(),
                                Finished = args.Status.Finished,
                                LastUpdate = DateTime.UtcNow
                            });
                        }
                    }
                    else
                    {
                        userFilesStatus = new UserFilesStatus()
                        {
                            UserName = args.UserName,
                            Files = new List<FileStatus>() {
                            new FileStatus()
                            {
                                FileName = args.Status.FileName,
                                Total = args.Status.GetTotalCount(),
                                Processed = args.Status.GetProcessedCount(),
                                Finished = args.Status.Finished,
                                LastUpdate = DateTime.UtcNow
                            }
                        }
                        };
                    }

                    jsonContent = JsonConvert.SerializeObject(userFilesStatus);

                    using (var fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                    {
                        fileStream.SetLength(0);
                        using (var streamWriter = new StreamWriter(fileStream))
                        {
                            streamWriter.WriteLine(jsonContent);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Problems to update processor status:{e}");
            }
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
        public EventHandler<StatusEventArgs> StatusEventHandler { get; set; }
    }

    public class ThreadEventArgs : EventArgs
    {
        public string Name { get; set; }
    }

    public class StatusEventArgs : EventArgs
    {
        public string UserName { get; set; }
        public ProcessResult Status { get; set; }
    }

    public class UserFilesStatus
    {
        public string UserName { get; set; }
        public List<FileStatus> Files { get; set; }
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
