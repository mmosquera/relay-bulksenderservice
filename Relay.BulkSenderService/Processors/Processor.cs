﻿using Newtonsoft.Json;
using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace Relay.BulkSenderService.Processors
{
	public abstract class Processor
	{
		protected readonly ILog _logger;
		protected readonly IConfiguration _configuration;
		protected bool _stop;
		protected object _lockStop;

		public event EventHandler<ThreadEventArgs> ProcessFinished;

		public Processor(ILog logger, IConfiguration configuration)
		{
			_logger = logger;
			_configuration = configuration;
			_stop = false;
			_lockStop = new object();
		}

		public void DoWork(object stateInfo)
		{
			ProcessFinished += ((ThreadStateInfo)stateInfo).Handler;

			IUserConfiguration user = ((ThreadStateInfo)stateInfo).User;
			string fileName = ((ThreadStateInfo)stateInfo).FileName;
			var result = new ProcessResult();

			try
			{
				_logger.Debug($"Start to process {fileName} for User:{user.Name} in Thread:{Thread.CurrentThread.ManagedThreadId}");

				var ftpHelper = user.Ftp.GetFtpHelper(_logger);

				result.ErrorFileName = GetErrorsFileName(fileName, user);

				SendStartProcessEmail(fileName, user);

				UploadStartFileToFTP(fileName, user, ftpHelper);

				string resultFileName = Process(user, fileName, result);

				UploadErrosToFTP(result.ErrorFileName, user, ftpHelper);

				UploadResultsToFTP(resultFileName, user, ftpHelper);

				if (!string.IsNullOrEmpty(resultFileName))
				{
					File.Move(fileName, fileName.Replace(".processing", ".processed"));

					SendEndProcessEmail(fileName, user, result);
				}

				SendErrorEmail(fileName, user.AdminEmail, result);

				AddReportForFile(resultFileName, user);

				_logger.Debug($"Finish processing {fileName} for User:{user.Name} in Thread:{Thread.CurrentThread.ManagedThreadId}");
			}
			catch (Exception e)
			{
				_logger.Error($"ERROR GENERAL PROCESS -- {e}");
			}
			finally
			{
				var args = new ThreadEventArgs()
				{
					Name = user.Name
				};
				OnProcessFinished(args);
			}
		}

		private void AddReportForFile(string fileName, IUserConfiguration user)
		{
			if (string.IsNullOrEmpty(fileName) || user.Reports == null)
			{
				return;
			}

			string reportFileName = $@"{_configuration.ReportsFolder}\reports.{DateTime.UtcNow.ToString("yyyyMMdd")}.json";
			List<ReportExecution> allReports = new List<ReportExecution>();

			if (File.Exists(reportFileName))
			{
				string json = File.ReadAllText(reportFileName);

				List<ReportExecution> executions = JsonConvert.DeserializeObject<List<ReportExecution>>(json);

				allReports.AddRange(executions);
			}

			ITemplateConfiguration templateConfiguration = ((UserApiConfiguration)user).GetTemplateConfiguration(fileName);

			List<FileReportTypeConfiguration> reportTypes = user.Reports.ReportsList
				.OfType<FileReportTypeConfiguration>()
				.Where(x => x.Templates.Contains(templateConfiguration.TemplateName))
				.ToList();

			if (reportTypes != null && reportTypes.Count > 0)
			{
				foreach (FileReportTypeConfiguration reportType in reportTypes)
				{
					var reportExecution = new ReportExecution()
					{
						UserName = user.Name,
						ReportId = reportType.ReportId,
						FileName = Path.GetFileName(fileName),
						CreatedAt = DateTime.UtcNow,
						RunDate = DateTime.UtcNow.AddHours(reportType.OffsetHour),
						Processed = false
					};

					allReports.Add(reportExecution);
				}

				string reports = JsonConvert.SerializeObject(allReports);
				using (var streamWriter = new StreamWriter(reportFileName, false))
				{
					streamWriter.Write(reports);
				}
			}
		}

		private void UploadStartFileToFTP(string fileName, IUserConfiguration user, IFtpHelper ftpHelper)
		{

		}

		protected abstract string Process(IUserConfiguration user, string file, ProcessResult result);

		protected virtual void OnProcessFinished(ThreadEventArgs args)
		{
			ProcessFinished?.Invoke(this, args);
		}

		protected string GetAttachmentFile(string attachmentFile, string originalFile, UserApiConfiguration user)
		{
			var filePathHelper = new FilePathHelper(_configuration, user.Name);

			string localAttachmentFolder = filePathHelper.GetAttachmentsFilesFolder();
			string localAttachmentFile = $@"{localAttachmentFolder}\{attachmentFile}";

			if (File.Exists(localAttachmentFile))
			{
				return localAttachmentFile;
			}

			string ftpAttachmentFile = $@"{user.AttachmentsFolder}/{attachmentFile}";

			var ftpHelper = user.Ftp.GetFtpHelper(_logger);
			ftpHelper.DownloadFile(ftpAttachmentFile, localAttachmentFile);

			if (File.Exists(localAttachmentFile))
			{
				ftpHelper.DeleteFile(ftpAttachmentFile);
				return localAttachmentFile;
			}

			string zipAttachments = $@"{user.AttachmentsFolder}/{Path.GetFileNameWithoutExtension(originalFile)}.zip";
			string localZipAttachments = $@"{localAttachmentFolder}\{Path.GetFileNameWithoutExtension(originalFile)}.zip";

			// TODO: add retries.
			ftpHelper.DownloadFile(zipAttachments, localZipAttachments);

			if (File.Exists(localZipAttachments))
			{
				using (ZipArchive zipArchive = ZipFile.OpenRead(localZipAttachments))
				{
					foreach (ZipArchiveEntry entry in zipArchive.Entries)
					{
						string entryFileName = $@"{localAttachmentFolder}\{entry.Name}";
						entry.ExtractToFile(entryFileName, true);
					}
				}
				ftpHelper.DeleteFile(zipAttachments);
				File.Delete(localZipAttachments); //TODO add retries.
			}

			if (File.Exists(localAttachmentFile))
			{
				return localAttachmentFile;
			}

			return null;
		}

		private void UploadResultsToFTP(string file, IUserConfiguration user, IFtpHelper ftpHelper)
		{
			if (File.Exists(file) && user.Results != null)
			{
				_logger.Debug($"Start to process results for file {file}");

				var filePathHelper = new FilePathHelper(_configuration, user.Name);

				string resultsPath = filePathHelper.GetResultsFilesFolder();

				string resultsFilePath = user.Results.SaveAndGetName(file, resultsPath);

				string ftpFileName = $@"{user.Results.Folder}/{Path.GetFileName(resultsFilePath)}";

				if (!File.Exists(resultsFilePath))
				{
					resultsFilePath = file;
				}

				ftpHelper.UploadFile(resultsFilePath, ftpFileName);
			}
		}

		private void SendStartProcessEmail(string file, IUserConfiguration user)
		{
			if (user.AdminEmail != null && user.AdminEmail.HasStartEmail && user.AdminEmail.Emails.Count > 0)
			{
				_logger.Debug($"Send start to send email for file {file}");

				var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
				smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

				var mailMessage = new MailMessage();
				mailMessage.Subject = "Doppler Relay - Start process";
				mailMessage.From = new MailAddress("support@dopplerrelay.com", "Doppler Relay Support");
				foreach (string email in user.AdminEmail.Emails)
				{
					mailMessage.To.Add(email);
				}

				string body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\StartProcess.es.html");

				mailMessage.Body = string.Format(body, Path.GetFileName(file), user.GetUserDateTime().DateTime);

				try
				{
					smtpClient.Send(mailMessage);
				}
				catch (Exception e)
				{
					_logger.Error($"Error trying to send starting email -- {e}");
				}
			}
		}

		private void SendEndProcessEmail(string file, IUserConfiguration user, ProcessResult result)
		{
			if (user.AdminEmail != null && user.AdminEmail.HasEndEmail && user.AdminEmail.Emails.Count > 0)
			{
				_logger.Debug($"Send end process email for file {file}");

				var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
				smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

				var mailMessage = new MailMessage();
				mailMessage.Subject = "Doppler Relay - Complete process";
				mailMessage.From = new MailAddress("support@dopplerrelay.com", "Doppler Relay Support");
				foreach (string email in user.AdminEmail.Emails)
				{
					mailMessage.To.Add(email);
				}

				string body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\FinishProcess.es.html");

				mailMessage.Body = string.Format(body, Path.GetFileName(file), user.GetUserDateTime().DateTime, result.ProcessedCount, result.ErrorsCount);

				try
				{
					smtpClient.Send(mailMessage);
				}
				catch (Exception e)
				{
					_logger.Error($"Error trying to send end process email -- {e}");
				}
			}
		}

		private void SendErrorEmail(string file, AdminEmailConfiguration adminEmail, ProcessResult result)
		{
			if (result.Type != ResulType.PROCESS && adminEmail != null && adminEmail.HasErrorEmail && adminEmail.Emails.Count > 0)
			{
				_logger.Debug($"Send end process email for file {file}");

				var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
				smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

				var mailMessage = new MailMessage()
				{
					Subject = "DOPPLER RELAY PROCESS ERROR",
					From = new MailAddress("support@dopplerrelay.com", "Doppler Relay Support")
				};
				foreach (string email in adminEmail.Emails)
				{
					mailMessage.To.Add(email);
				}
				string body = null;
				switch (result.Type)
				{
					case ResulType.DOWNLOAD:
						body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorDownload.es.html");
						break;
					case ResulType.LOGIN:
						body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorLogin.es.html");
						break;
					case ResulType.UNZIP:
						body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorUnzip.es.html");
						break;
					case ResulType.REPEATED:
						body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorRepeated.es.html");
						break;
				}

				if (string.IsNullOrEmpty(body))
				{
					body = "Error processing file {0}.";
				}

				mailMessage.Body = string.Format(body, Path.GetFileName(file));

				try
				{
					smtpClient.Send(mailMessage);
				}
				catch (Exception e)
				{
					_logger.Error($"Error trying to send error process email -- {e}");
				}
			}
		}

		private void UploadErrosToFTP(string fileName, IUserConfiguration user, IFtpHelper ftpHelper)
		{
			if (user.Errors != null && File.Exists(fileName))
			{
				_logger.Debug($"Upload error file {fileName}");

				var filePathHelper = new FilePathHelper(_configuration, user.Name);

				string ftpFileName = $@"{user.Errors.Folder}/{Path.GetFileName(fileName)}";

				ftpHelper.UploadFile(fileName, ftpFileName);
			}
		}

		private string GetErrorsFileName(string file, IUserConfiguration user)
		{
			string errorsFilePath = null;

			if (user.Errors != null)
			{
				var filePathHelper = new FilePathHelper(_configuration, user.Name);

				string errorsPath = filePathHelper.GetResultsFilesFolder();

				errorsFilePath = $@"{errorsPath}\{user.Errors.Name.GetReportName(file, errorsPath)}";
			}

			return errorsFilePath;
		}

		public void Processor_StopSendEvent(object sender, CommandsEventArgs e)
		{
			lock (_lockStop)
			{
				_stop = true;
			}
		}

		protected bool MustStop()
		{
			lock (_lockStop)
			{
				return _stop;
			}
		}
	}
}
