using System;
using System.IO;

namespace Relay.BulkSenderService.Processors
{
	/// <summary>
	/// Read commands from file commands.txt. Read one command on first line.
	/// There are 3 commands separted with whitespaces ( ). 
	///  - date
	///  - user
	///  - args
	/// Args are parameters separated with commas (,)
	/// </summary>
	public class FileCommandsWatcher : IWatcher
	{
		private FileSystemWatcher _fileSystemWatcher;
		private const string FILENAME = "commands.txt";

		public event EventHandler<CommandsEventArgs> StartProcessEvent;
		public event EventHandler<CommandsEventArgs> StopProcessEvent;
		public event EventHandler<CommandsEventArgs> AddThreadEvent;
		public event EventHandler<CommandsEventArgs> RemoveThreadEvent;
		public event EventHandler<CommandsEventArgs> StopSendEvent;
		public event EventHandler<ReportCommandsEventArgs> GenerateReportEvent;
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

		protected virtual void OnGenerateReport(ReportCommandsEventArgs args)
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
						var startProcessArgs = new CommandsEventArgs()
						{
							User = commandParams
						};
						OnStartProcess(startProcessArgs);
						break;
					case "STOPPROCESS":
						var stopProcessArgs = new CommandsEventArgs()
						{
							User = commandParams
						};
						OnStopProcess(stopProcessArgs);
						break;
					case "ADDTHREAD":
						var addThreadArgs = new CommandsEventArgs()
						{
							User = commandParams
						};
						OnAddThread(addThreadArgs);
						break;
					case "REMOVETHREAD":
						var removeThreadArgs = new CommandsEventArgs()
						{
							User = commandParams
						};
						OnRemoveThread(removeThreadArgs);
						break;
					case "CHANGECONFIGURATION":
						OnChangeConfiguration();
						break;
					case "STOPSEND":
						var stopSendArgs = new CommandsEventArgs()
						{
							User = commandParams
						};
						OnStopSend(stopSendArgs);
						break;
					case "GENERATEREPORT":
						ReportCommandsEventArgs reportArgs = GetReportCommandsEventArgs(commandParams);
						OnGenerateReport(reportArgs);
						break;
				}
			}
		}

		private ReportCommandsEventArgs GetReportCommandsEventArgs(string commandParams)
		{
			string[] paramArray = commandParams.Split(',');
			var reportArgs = new ReportCommandsEventArgs();

			reportArgs.User = paramArray[0] != null ? paramArray[0] : string.Empty;
			reportArgs.Report = paramArray[1] != null ? paramArray[1] : string.Empty;
			reportArgs.Start = paramArray[2] != null ? DateTime.Parse(paramArray[2]) : DateTime.MinValue;
			reportArgs.End = paramArray[3] != null ? DateTime.Parse(paramArray[3]) : DateTime.MinValue;

			return reportArgs;
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
	}

	public class CommandsEventArgs : EventArgs
	{
		public string User { get; set; }
	}

	public class ReportCommandsEventArgs : CommandsEventArgs
	{
		public string Report { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
	}
}
