using Newtonsoft.Json;
using System;
using System.IO;

namespace Relay.BulkSenderService.Processors
{
    public class FileCommandsWatcher : IWatcher
    {
        //private Commands _commands;
        private FileSystemWatcher _fileSystemWatcher;
        private const string FILENAME = "commands.txt";

        public event EventHandler<CommandsEventArgs> StartProcessEvent;
        public event EventHandler<CommandsEventArgs> StopProcessEvent;
        public event EventHandler<CommandsEventArgs> AddThreadEvent;
        public event EventHandler<CommandsEventArgs> RemoveThreadEvent;
        public event EventHandler<CommandsEventArgs> StopSendEvent;
        public event EventHandler<CommandsEventArgs> GenerateReportEvent;
        public event EventHandler ChangeConfigurationEvent;

        public FileCommandsWatcher()
        {
            //_commands = GetCommands();
            _fileSystemWatcher = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory, FILENAME);
            _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _fileSystemWatcher.Changed += FileCommandsWatcher_Changed;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        protected virtual void OnStartProcess(CommandsEventArgs args)
        {
            StartProcessEvent?.Invoke(this, args);
        }

        protected virtual void OnStopProcess(CommandsEventArgs args)
        {
            StopProcessEvent?.Invoke(this, args);
        }

        protected virtual void OnAddThread(CommandsEventArgs args)
        {
            AddThreadEvent?.Invoke(this, args);
        }

        protected virtual void OnRemoveThread(CommandsEventArgs args)
        {
            RemoveThreadEvent?.Invoke(this, args);
        }

        protected virtual void OnChangeConfiguration()
        {
            ChangeConfigurationEvent?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnStopSend(CommandsEventArgs args)
        {
            StopSendEvent?.Invoke(this, args);
        }

        protected virtual void OnGenerateReport(CommandsEventArgs args)
        {
            GenerateReportEvent?.Invoke(this, args);
        }

        private DateTime lastRead = DateTime.MinValue;

        private bool IsDuplicatedEvent()
        {
            DateTime lastWrite = File.GetLastWriteTime($"{AppDomain.CurrentDomain.BaseDirectory}{FILENAME}");
            if (lastWrite != lastRead)
            {
                lastRead = lastWrite;
                return false;
            }
            else
            {
                return true;
            }
        }

        private void FileCommandsWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (IsDuplicatedEvent())
            {
                return;
            }
            //_fileSystemWatcher.EnableRaisingEvents = false;
            //Commands newCommands = GetCommands();
            string command = GetCommnd();

            if (string.IsNullOrEmpty(command))
            {
                return;
            }

            string[] commandArray = command.Split(' ');

            if (commandArray.Length == 3)
            {
                string commandDate = commandArray[0];
                string commandName = commandArray[1];
                string commandParams = commandArray[2];

                switch (commandName.ToUpper())
                {
                    case "STARTPROCESS":
                        var args = new CommandsEventArgs()
                        {
                            User = commandParams
                        };
                        OnStartProcess(args);
                        break;
                    case "STOPPROCESS":
                        args = new CommandsEventArgs()
                        {
                            User = commandParams
                        };
                        OnStopProcess(args);
                        break;
                    case "ADDTHREAD":
                        args = new CommandsEventArgs()
                        {
                            User = commandParams
                        };
                        OnAddThread(args);
                        break;
                    case "REMOVETHREAD":
                        args = new CommandsEventArgs()
                        {
                            User = commandParams
                        };
                        OnRemoveThread(args);
                        break;
                    case "CHANGECONFIGURATION":
                        OnChangeConfiguration();
                        break;
                    case "STOPSEND":
                        args = new CommandsEventArgs()
                        {
                            User = commandParams
                        };
                        OnStopSend(args);
                        break;
                    case "GENERATEREPORT":
                        args = new CommandsEventArgs()
                        {
                            User = commandParams
                        };
                        OnGenerateReport(args);
                        break;
                }
            }

            //_fileSystemWatcher.EnableRaisingEvents = true;
        }

        private string GetCommnd()
        {
            string filePath = $"{AppDomain.CurrentDomain.BaseDirectory}{FILENAME}";
            try
            {
                using (StreamReader streamReader = new StreamReader(filePath))
                {
                    return streamReader.ReadLine();
                }
            }
            catch (IOException)
            {
                return null;
            }
        }

        private Commands GetCommands()
        {
            string filePath = $"{AppDomain.CurrentDomain.BaseDirectory}{FILENAME}";

            bool read = false;
            string json = "";
            while (!read)
            {
                try
                {
                    json = File.ReadAllText(filePath);
                    read = true;
                }
                catch (IOException) { }
            }

            Commands commands = JsonConvert.DeserializeObject<Commands>(json);

            return commands;
        }
    }

    public class Commands
    {
        [JsonProperty(PropertyName = "stopProcess")]
        public bool StopProcess { get; set; }

        [JsonProperty(PropertyName = "changeConfiguration")]
        public bool ChangeConfiguration { get; set; }

        [JsonProperty(PropertyName = "executeFile")]
        public string ExecuteFile { get; set; }

        [JsonProperty(PropertyName = "freeUser")]
        public string FreeUser { get; set; }
    }

    public class CommandsEventArgs : EventArgs
    {
        public string User { get; set; }
    }
}
