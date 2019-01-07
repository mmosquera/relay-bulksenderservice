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
		public CleanProcessor(ILog logger, IConfiguration configuration, IWatcher watcher) : base(logger, configuration, watcher)
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

						DeleteLocalFiles(user);
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

			DirectoryInfo directory = new DirectoryInfo(attachmentsFolder);
			if (directory.Exists)
			{
				FileInfo[] files = directory.GetFiles().Where(f => f.CreationTimeUtc < filterDate).ToArray();
				foreach (FileInfo file in files)
				{
					try
					{
						file.Delete();
					}
					catch (Exception e)
					{
						_logger.Error($"Error trying to delete attach file {file.FullName} -- {e}");
					}
				}
			}
		}

		private void DeleteLocalFiles(IUserConfiguration user)
		{
			string path = new FilePathHelper(_configuration, user.Name).GetUserFolder();

			DateTime filterDate = DateTime.UtcNow.AddDays(-_configuration.CleanDays);

			var baseDirectory = new DirectoryInfo(path);
			if (baseDirectory.Exists)
			{
				DirectoryInfo[] directories = baseDirectory.GetDirectories();
				foreach (DirectoryInfo directory in directories)
				{
					FileInfo[] files = directory.GetFiles().Where(f => f.CreationTimeUtc < filterDate).ToArray();
					foreach (FileInfo file in files)
					{
						try
						{
							file.Delete();
						}
						catch (Exception e)
						{
							_logger.Error($"Error trying to delete file {file.FullName} -- {e}");
						}
					}
				}
			}
		}

		private void DeleteReportsFiles()
		{
			DateTime filterDate = DateTime.UtcNow.AddDays(-_configuration.CleanDays);

			var directory = new DirectoryInfo(_configuration.ReportsFolder);
			if (directory.Exists)
			{
				FileInfo[] files = directory.GetFiles().Where(f => f.CreationTimeUtc < filterDate).ToArray();
				foreach (FileInfo file in files)
				{
					try
					{
						file.Delete();
					}
					catch (Exception e)
					{
						_logger.Error($"Error trying to delete file {file.FullName} -- {e}");
					}
				}
			}
		}
	}
}
