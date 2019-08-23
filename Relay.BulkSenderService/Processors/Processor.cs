using Newtonsoft.Json;
using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Configuration.Alerts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;

namespace Relay.BulkSenderService.Processors
{
    public abstract class Processor
    {
        protected readonly ILog _logger;
        protected readonly IConfiguration _configuration;
        protected bool _stop;
        protected object _lockStop;
        protected int _lineNumber;
        private DateTime _lastStatusDate;
        private const int STATUS_MINUTES = 5;

        public event EventHandler<ThreadEventArgs> ProcessFinished;
        public event EventHandler<StatusEventArgs> ProcessStatus;

        public Processor(ILog logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _stop = false;
            _lockStop = new object();
            _lineNumber = 0;
            _lastStatusDate = DateTime.MinValue;
        }

        public void DoWork(object stateInfo)
        {
            ProcessFinished += ((ThreadStateInfo)stateInfo).Handler;
            ProcessStatus += ((ThreadStateInfo)stateInfo).StatusEventHandler;

            IUserConfiguration user = ((ThreadStateInfo)stateInfo).User;
            string fileName = ((ThreadStateInfo)stateInfo).FileName;
            var result = new ProcessResult()
            {
                FileName = Path.GetFileNameWithoutExtension(fileName)
            };

            try
            {
                _logger.Debug($"Start to process {fileName} for User:{user.Name} in Thread:{Thread.CurrentThread.ManagedThreadId}");

                SendStartProcessEmail(fileName, user);

                string resultFileName = Process(user, fileName, result);

                GenerateErrorFile(fileName, user, result);

                var ftpHelper = user.Ftp.GetFtpHelper(_logger);

                UploadErrosToFTP(result.ErrorFileName, user, ftpHelper);

                UploadResultsToFTP(resultFileName, user, ftpHelper);

                if (!string.IsNullOrEmpty(resultFileName))
                {
                    File.Move(fileName, fileName.Replace(".processing", ".processed"));

                    SendEndProcessEmail(fileName, user, result);
                }

                SendErrorEmail(fileName, user.Alerts, result);

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

        private void GenerateErrorFile(string fileName, IUserConfiguration user, ProcessResult processResult)
        {
            if (processResult.Errors.Count == 0)
            {
                return;
            }

            processResult.ErrorFileName = GetErrorsFileName(fileName, user);

            var stringBuilder = new StringBuilder();
            foreach (ProcessError processError in processResult.Errors.OrderBy(x => x.LineNumber))
            {
                stringBuilder.AppendLine(processError.GetErrorLine());
            }

            using (var streamWriter = new StreamWriter(processResult.ErrorFileName))
            {
                streamWriter.Write(stringBuilder);
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

        protected abstract string Process(IUserConfiguration user, string file, ProcessResult result);

        protected virtual void OnProcessFinished(ThreadEventArgs args)
        {
            ProcessFinished?.Invoke(this, args);
        }

        protected virtual void OnProcessStatus(StatusEventArgs args)
        {
            ProcessStatus?.Invoke(this, args);
        }
        
        protected void GetProcessStatus(IUserConfiguration user, ProcessResult processResult)
        {
            if (user.Status == null || !processResult.Finished && DateTime.Now.Subtract(_lastStatusDate).TotalMinutes < STATUS_MINUTES)
            {
                return;
            }

            var statusEventArgs = new StatusEventArgs()
            {
                UserName = user.Name,
                Status = processResult
            };

            OnProcessStatus(statusEventArgs);

            _lastStatusDate = DateTime.Now;
        }

        protected string GetAttachmentFile(string attachmentFile, string originalFile, IUserConfiguration user)
        {
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            //local file 
            string localAttachmentFolder = filePathHelper.GetAttachmentsFilesFolder();
            string localAttachmentFile = $@"{localAttachmentFolder}\{attachmentFile}";

            if (File.Exists(localAttachmentFile))
            {
                return localAttachmentFile;
            }

            //local file in subfolder
            string subFolder = Path.GetFileNameWithoutExtension(originalFile);
            string localAttachmentSubFile = $@"{localAttachmentFolder}\{subFolder}\{attachmentFile}";

            if (File.Exists(localAttachmentSubFile))
            {
                return localAttachmentSubFile;
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
                string newZipDirectory = $@"{localAttachmentFolder}\{subFolder}";
                Directory.CreateDirectory(newZipDirectory);

                var zipHelper = new ZipHelper();
                zipHelper.UnzipFile(localZipAttachments, newZipDirectory);

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
            if (user.Alerts != null && user.Alerts.GetStartAlert() != null && user.Alerts.Emails.Count > 0)
            {
                var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
                smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

                var mailMessage = new MailMessage();
                mailMessage.Subject = user.Alerts.GetStartAlert().Subject;
                mailMessage.From = new MailAddress("support@dopplerrelay.com", "Doppler Relay Support");

                foreach (string email in user.Alerts.Emails)
                {
                    mailMessage.To.Add(email);
                }

                string body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\StartProcess.es.html");

                mailMessage.Body = body.Replace("{{filename}}", Path.GetFileNameWithoutExtension(file)).Replace("{{time}}", user.GetUserDateTime().DateTime.ToString());
                mailMessage.IsBodyHtml = true;

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
            if (user.Alerts != null && user.Alerts.GetEndAlert() != null && user.Alerts.Emails.Count > 0)
            {
                var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
                smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

                var mailMessage = new MailMessage();
                mailMessage.Subject = user.Alerts.GetEndAlert().Subject;
                mailMessage.From = new MailAddress("support@dopplerrelay.com", "Doppler Relay Support");

                foreach (string email in user.Alerts.Emails)
                {
                    mailMessage.To.Add(email);
                }

                mailMessage.Body = GetBody(file, user, result);
                mailMessage.IsBodyHtml = true;

                List<string> attachments = GetAttachments(file, user.Name);

                foreach (string attachment in attachments)
                {
                    mailMessage.Attachments.Add(new Attachment(attachment)
                    {
                        Name = Path.GetFileName(attachment)
                    });
                }

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

        protected virtual string GetHeaderLine(string line, ITemplateConfiguration templateConfiguration)
        {
            if (templateConfiguration != null && !templateConfiguration.HasHeaders)
            {
                return string.Join(templateConfiguration.FieldSeparator.ToString(), templateConfiguration.Fields.Select(x => x.Name));
            }

            return line;
        }

        protected string GetResultsFileName(string fileName, IUserConfiguration user)
        {
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            string resultsFileName = $@"{filePathHelper.GetResultsFilesFolder()}\{fileName.Replace(".processing", ".sent")}";

            return resultsFileName;
        }

        protected abstract List<string> GetAttachments(string file, string userName);

        protected abstract string GetBody(string file, IUserConfiguration user, ProcessResult result);

        private void SendErrorEmail(string file, AlertConfiguration alerts, ProcessResult result)
        {
            if (result.Errors.Any(x => x.Type != ErrorType.PROCESS)
                && alerts != null
                && alerts.GetErrorAlert() != null
                && alerts.Emails.Count > 0)
            {
                var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
                smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

                var mailMessage = new MailMessage()
                {
                    Subject = alerts.GetErrorAlert().Subject,
                    From = new MailAddress("support@dopplerrelay.com", "Doppler Relay Support")
                };
                foreach (string email in alerts.Emails)
                {
                    mailMessage.To.Add(email);
                }

                string body = null;

                ProcessError error = result.Errors.FirstOrDefault(x => x.Type != ErrorType.PROCESS);

                switch (error.Type)
                {
                    case ErrorType.DOWNLOAD:
                        body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorDownload.es.html");
                        break;
                    case ErrorType.LOGIN:
                        body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorLogin.es.html");
                        break;
                    case ErrorType.UNZIP:
                        body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorUnzip.es.html");
                        break;
                    case ErrorType.REPEATED:
                        body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorRepeated.es.html");
                        break;
                }

                if (string.IsNullOrEmpty(body))
                {
                    body = "Error processing file {{filename}}.";
                }

                mailMessage.Body = body.Replace("{{filename}}", Path.GetFileNameWithoutExtension(file));
                mailMessage.IsBodyHtml = true;

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

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            string errorsPath = filePathHelper.GetResultsFilesFolder();

            if (user.Errors != null)
            {
                errorsFilePath = $@"{errorsPath}\{user.Errors.Name.GetReportName(file, errorsPath)}";
            }
            else
            {
                errorsFilePath = $@"{errorsPath}\{Path.GetFileNameWithoutExtension(file)}.error";
            }

            return errorsFilePath;
        }

        protected int GetTotalLines(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return 0;
            }

            int totalLines = 0;

            using (var streamReader = new StreamReader(fileName))
            {
                while (streamReader.ReadLine() != null)
                {
                    totalLines++;
                }
            }

            return totalLines;
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
