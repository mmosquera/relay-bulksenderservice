using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Configuration
{
	public class UserApiConfiguration : IUserConfiguration
	{
		public int FtpInterval { get; set; }
		public bool HasDeleteFtp { get; set; }
		public string Name { get; set; }
		public int UserGMT { get; set; }
		public string AttachmentsFolder { get; set; }
		public AckConfiguration Ack { get; set; }
		public ErrorConfiguration Errors { get; set; }
		public IResultConfiguration Results { get; set; }
		public AdminEmailConfiguration AdminEmail { get; set; }
		public List<string> DownloadFolders { get; set; }
		public List<string> FileExtensions { get; set; }
		public List<ITemplateConfiguration> Templates { get; set; }
		public CredentialsConfiguration Credentials { get; set; }
		public IFtpConfiguration Ftp { get; set; }
		public ReportConfiguration Reports { get; set; }
		public AlertConfiguration Alerts { get; set; }

		public DateTimeOffset GetUserDateTime()
		{
			var timeSpan = new TimeSpan(UserGMT, 0, 0);

			return new DateTimeOffset(DateTimeOffset.UtcNow.Add(timeSpan).DateTime, timeSpan);
		}

		public Processor GetProcessor(ILog logger, IConfiguration configuration, string fileName)
		{
			// TODO: validate null value.
			return GetTemplateConfiguration(fileName)?.GetProcessor(logger, configuration);
		}

		public ITemplateConfiguration GetTemplateConfiguration(string fileName)
		{
			string name = Path.GetFileNameWithoutExtension(fileName);
			foreach (ITemplateConfiguration templateConfiguration in this.Templates)
			{
				string[] namePartsArray = name.ToUpper().Split(templateConfiguration.FileNamePartSeparator);

				if (templateConfiguration.FileNameParts.All(x => namePartsArray.Contains(x.ToUpper())))
				{
					return templateConfiguration;
				}
			}

			return Templates.Where(x => x.FileNameParts.Contains("*")).FirstOrDefault();
		}

		public IUserConfiguration Clone()
		{
			var configuration = new UserApiConfiguration();

			configuration.FtpInterval = this.FtpInterval;
			configuration.HasDeleteFtp = this.HasDeleteFtp;
			configuration.Name = this.Name;
			configuration.UserGMT = this.UserGMT;
			configuration.AttachmentsFolder = this.AttachmentsFolder;

			if (this.Errors != null)
			{
				configuration.Errors = Errors.Clone();
			}

			if (this.Results != null)
			{
				configuration.Results = this.Results.Clone();
			}

			if (this.AdminEmail != null)
			{
				configuration.AdminEmail = this.AdminEmail.Clone();
			}

			if (this.DownloadFolders != null)
			{
				configuration.DownloadFolders = new List<string>();

				foreach (string downloadFolder in this.DownloadFolders)
				{
					configuration.DownloadFolders.Add(downloadFolder);
				}
			}

			if (this.FileExtensions != null)
			{
				configuration.FileExtensions = new List<string>();

				foreach (string fileExtension in this.FileExtensions)
				{
					configuration.FileExtensions.Add(fileExtension);
				}
			}

			if (this.Templates != null)
			{
				configuration.Templates = new List<ITemplateConfiguration>();

				foreach (ITemplateConfiguration template in this.Templates)
				{
					configuration.Templates.Add(template.Clone());
				}
			}

			if (this.Ack != null)
			{
				configuration.Ack = this.Ack.Clone();
			}

			if (this.Credentials != null)
			{
				configuration.Credentials = this.Credentials.Clone();
			}

			if (this.Ftp != null)
			{
				configuration.Ftp = this.Ftp.Clone();
			}

			if (this.Reports != null)
			{
				configuration.Reports = this.Reports.Clone();
			}

			if (this.Alerts != null)
			{
				configuration.Alerts = this.Alerts.Clone();
			}

			return configuration;
		}
	}
}
