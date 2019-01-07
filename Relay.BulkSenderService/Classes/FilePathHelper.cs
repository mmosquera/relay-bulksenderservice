using Relay.BulkSenderService.Configuration;
using System.IO;

namespace Relay.BulkSenderService.Classes
{
	public class FilePathHelper
	{
		private IConfiguration _configuration;
		private string _userName;

		public FilePathHelper(IConfiguration configuration, string userName)
		{
			_configuration = configuration;
			_userName = userName;
		}

		public string GetUserFolder()
		{
			return $@"{_configuration.LocalDownloadFolder}\{_userName}";
		}

		public string GetProcessedFilesFolder()
		{
			return $@"{GetUserFolder()}\Processed";
		}

		public string GetReportsFilesFolder()
		{
			return $@"{GetUserFolder()}\Reports";
		}

		public string GetResultsFilesFolder()
		{
			return $@"{GetUserFolder()}\Results";
		}

		public string GetAttachmentsFilesFolder()
		{
			return $@"{GetUserFolder()}\Attachments";
		}

		public string GetRetriesFilesFolder()
		{
			return $@"{GetUserFolder()}\Retries";
		}

		public string GetForcedReportsFolder()
		{
			return $@"{GetUserFolder()}\ForcedReports";
		}

		public string GetDownloadsFolder()
		{
			return $@"{GetUserFolder()}\Downloads";
		}

		public void CreateUserFolders()
		{
			if (!Directory.Exists(GetUserFolder()))
			{
				Directory.CreateDirectory(GetUserFolder());
			}

			if (!Directory.Exists(GetProcessedFilesFolder()))
			{
				Directory.CreateDirectory(GetProcessedFilesFolder());
			}

			if (!Directory.Exists(GetReportsFilesFolder()))
			{
				Directory.CreateDirectory(GetReportsFilesFolder());
			}

			if (!Directory.Exists(GetResultsFilesFolder()))
			{
				Directory.CreateDirectory(GetResultsFilesFolder());
			}

			if (!Directory.Exists(GetAttachmentsFilesFolder()))
			{
				Directory.CreateDirectory(GetAttachmentsFilesFolder());
			}

			if (!Directory.Exists(GetRetriesFilesFolder()))
			{
				Directory.CreateDirectory(GetRetriesFilesFolder());
			}

			if (!Directory.Exists(GetDownloadsFolder()))
			{
				Directory.CreateDirectory(GetDownloadsFolder());
			}
		}
	}
}
