﻿using Newtonsoft.Json;
using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Processors.Errors;
using Relay.BulkSenderService.Queues;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.BulkSenderService.Processors
{
    public abstract class Processor
    {
        protected readonly ILog _logger;
        protected readonly IConfiguration _configuration;
        protected int _lineNumber;
        private DateTime _lastStatusDate;
        private const int STATUS_MINUTES = 5;
        private const int WAITING_CONSUMERS_TIME = 1000;
        private IBulkQueue outboundQueue;
        private FileWriter resultFileWriter;
        private FileWriter errorFileWriter;

        public event EventHandler<ThreadEventArgs> ProcessFinished;

        private int _total;
        private int _processed;
        private object _lockProcessed;
        private int _errors;

        public Processor(ILog logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _lineNumber = 0;
            _lastStatusDate = DateTime.MinValue;
            _total = 0;
            _processed = 0;
            _lockProcessed = new object();
        }

        public void DoWork(object stateInfo)
        {
            ProcessFinished += ((ThreadStateInfo)stateInfo).Handler;

            IUserConfiguration user = ((ThreadStateInfo)stateInfo).User;
            string fileName = ((ThreadStateInfo)stateInfo).FileName;

            try
            {
                _logger.Debug($"Start to process {fileName} for User:{user.Name} in Thread:{Thread.CurrentThread.ManagedThreadId}");

                if (!ValidateCredentials(user.Credentials))
                {
                    _logger.Error($"Error to authenticate user:{user.Name}");

                    new LoginError(_configuration).SendErrorEmail(fileName, user.Alerts);

                    return;
                }

                SendStartProcessEmail(fileName, user);

                _total = GetTotalLines(user, fileName);

                ProcessFile(user, fileName);

                string resultFileName = GenerateResultFile(fileName, user);
                string errorFileName = GenerateErrorFile(fileName, user);

                var ftpHelper = user.Ftp.GetFtpHelper(_logger);

                UploadErrosToFTP(errorFileName, user, ftpHelper);

                UploadResultsToFTP(resultFileName, user, ftpHelper);

                if (!string.IsNullOrEmpty(resultFileName))
                {
                    File.Move(fileName, fileName.Replace(".processing", ".processed"));

                    SendEndProcessEmail(fileName, user);
                }

                AddReportForFile(resultFileName, user);

                RemoveQueues(fileName, user);

                _logger.Debug($"Finish processing {fileName} for User:{user.Name} in Thread:{Thread.CurrentThread.ManagedThreadId} at:{DateTime.UtcNow}");
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

        private void RemoveQueues(string fileName, IUserConfiguration userConfiguration)
        {
            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            try
            {
                string errorFileName = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.error.tmp";
                if (File.Exists(errorFileName))
                {
                    File.Delete(errorFileName);
                }

                string resultFileName = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.result.tmp";
                if (File.Exists(resultFileName))
                {
                    File.Delete(resultFileName);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error trying to delete queues for file:{fileName} -- {e}");
            }
        }

        private string GenerateResultFile(string fileName, IUserConfiguration user)
        {
            int lineNumber = 0;
            var errors = GetErrorsFromFile(user, fileName);
            var results = GetResultsFromFile(user, fileName);
            var templateConfiguration = user.GetTemplateConfiguration(fileName);

            var sent = new StringBuilder();

            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            {
                string line = null;

                if (templateConfiguration.HasHeaders)
                {
                    line = streamReader.ReadLine();
                    lineNumber++;
                }

                string headers = GetHeaderLine(line, templateConfiguration);
                string resultHeaders = $"{headers}{templateConfiguration.FieldSeparator}{Constants.HEADER_PROCESS_RESULT}{templateConfiguration.FieldSeparator}{Constants.HEADER_DELIVERY_RESULT}{templateConfiguration.FieldSeparator}{Constants.HEADER_MESSAGE_ID}{templateConfiguration.FieldSeparator}{Constants.HEADER_DELIVERY_LINK}";

                sent.AppendLine(resultHeaders);

                while (!streamReader.EndOfStream)
                {
                    line = streamReader.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        lineNumber++;
                        continue;
                    }

                    string resultLine = string.Empty;

                    ProcessResult processResult = results.FirstOrDefault(x => x.LineNumber == lineNumber);

                    if (processResult != null)
                    {
                        resultLine = $"{processResult.GetResultLine(templateConfiguration.FieldSeparator)}";
                    }

                    ProcessError processError = errors.FirstOrDefault(x => x.LineNumber == lineNumber);

                    if (processError != null)
                    {
                        resultLine = $"{processError.GetErrorLineResult(templateConfiguration.FieldSeparator)}";
                    }

                    if (!string.IsNullOrEmpty(resultLine))
                    {
                        line = $"{line}{templateConfiguration.FieldSeparator}{resultLine}";
                        sent.AppendLine(line);
                    }

                    lineNumber++;
                }
            }

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            string resultFileName = $@"{filePathHelper.GetResultsFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.sent";

            using (var streamWriter = new StreamWriter(resultFileName))
            {
                streamWriter.Write(sent);
            }

            return resultFileName;
        }

        private string GenerateErrorFile(string fileName, IUserConfiguration user)
        {
            var errors = GetErrorsFromFile(user, fileName);

            if (errors.Count == 0)
            {
                return null;
            }

            string errorFileName = GetErrorsFileName(fileName, user);

            var stringBuilder = new StringBuilder();

            foreach (ProcessError processError in errors.OrderBy(x => x.LineNumber))
            {
                stringBuilder.AppendLine(processError.GetErrorLine());
            }

            using (var streamWriter = new StreamWriter(errorFileName))
            {
                streamWriter.Write(stringBuilder);
            }

            return errorFileName;
        }

        private void UpdateStatusFile(string fileName, IUserConfiguration user, CancellationToken cancellationToken)
        {
            var filePathHelper = new FilePathHelper(_configuration, user.Name);
            string statusFileName = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.status.tmp";

            var fileWriter = new FileWriter(statusFileName);

            bool finished = false;

            while (!finished)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    finished = true;
                }

                if (DateTime.Now.Subtract(_lastStatusDate).TotalMinutes > STATUS_MINUTES || finished)
                {
                    var filesStatus = new FileStatus()
                    {
                        FileName = fileName,
                        Finished = finished,
                        Total = _total,
                        LastUpdate = DateTime.UtcNow
                    };

                    lock (_lockProcessed)
                    {
                        filesStatus.Processed = _processed;
                    }

                    string jsonContent = JsonConvert.SerializeObject(filesStatus, Formatting.None);

                    fileWriter.WriteFile(jsonContent);

                    _lastStatusDate = DateTime.Now;
                }

                Thread.Sleep(5000);
            }
        }

        private List<ProcessError> GetErrorsFromFile(IUserConfiguration userConfiguration, string fileName)
        {
            var errors = new List<ProcessError>();

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);
            string errorQueue = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.error.tmp";

            if (!File.Exists(errorQueue))
            {
                return errors;
            }

            using (var fileStream = new FileStream(errorQueue, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            {
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();

                    ProcessError processError = JsonConvert.DeserializeObject<ProcessError>(line);

                    errors.Add(processError);
                }
            }

            return errors;
        }

        private List<ProcessResult> GetResultsFromFile(IUserConfiguration userConfiguration, string fileName)
        {
            var results = new List<ProcessResult>();

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);
            string resultQueue = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.result.tmp";

            if (!File.Exists(resultQueue))
            {
                return results;
            }

            using (var fileStream = new FileStream(resultQueue, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            {
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();

                    ProcessResult processResult = JsonConvert.DeserializeObject<ProcessResult>(line);

                    results.Add(processResult);
                }
            }

            return results;
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

        protected virtual void OnProcessFinished(ThreadEventArgs args)
        {
            ProcessFinished?.Invoke(this, args);
        }

        protected string GetAttachmentFile(string attachmentFile, string originalFile, IUserConfiguration user)
        {
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            string localAttachmentFolder = filePathHelper.GetAttachmentsFilesFolder();

            //1- local file 
            string localAttachmentFile = $@"{localAttachmentFolder}\{attachmentFile}";

            if (File.Exists(localAttachmentFile))
            {
                return localAttachmentFile;
            }

            //2- local file in subfolder
            string subFolder = Path.GetFileNameWithoutExtension(originalFile);
            string localAttachmentSubFile = $@"{localAttachmentFolder}\{subFolder}\{attachmentFile}";

            if (File.Exists(localAttachmentSubFile))
            {
                return localAttachmentSubFile;
            }

            ITemplateConfiguration templateConfiguration = user.GetTemplateConfiguration(originalFile);

            //3- donwload from ftp
            string ftpAttachmentFile = $@"{templateConfiguration.AttachmentsFolder}/{attachmentFile}";

            var ftpHelper = user.Ftp.GetFtpHelper(_logger);
            ftpHelper.DownloadFile(ftpAttachmentFile, localAttachmentFile);

            if (File.Exists(localAttachmentFile))
            {
                ftpHelper.DeleteFile(ftpAttachmentFile);
                return localAttachmentFile;
            }

            //4- zip file 
            string zipAttachments = $@"{templateConfiguration.AttachmentsFolder}/{Path.GetFileNameWithoutExtension(originalFile)}.zip";
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

            if (File.Exists(localAttachmentSubFile))
            {
                return localAttachmentSubFile;
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

        private void SendEndProcessEmail(string file, IUserConfiguration user)
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

                mailMessage.Body = GetBody(file, user, _processed, _errors);
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

        protected abstract List<string> GetAttachments(string file, string userName);

        protected abstract string GetBody(string file, IUserConfiguration user, int processedCount, int errorsCount);

        //body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorUnzip.es.html");        

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

        private int GetTotalLines(IUserConfiguration userConfiguration, string fileName)
        {
            if (!File.Exists(fileName))
            {
                return 0;
            }

            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(fileName);

            if (templateConfiguration == null)
            {
                return 0;
            }

            int totalLines = 0;

            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        totalLines++;
                    }
                }
            }

            if (templateConfiguration.HasHeaders)
            {
                totalLines--;
            }

            return totalLines;
        }

        private void ProcessFile(IUserConfiguration userConfiguration, string fileName)
        {
            outboundQueue = new MemoryBulkQueue();

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            string errorFileName = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.error.tmp";
            errorFileWriter = new FileWriter(errorFileName);

            string resultFileName = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.result.tmp";
            resultFileWriter = new FileWriter(resultFileName);

            //get for retries.
            //TODO: capaz que hay que hacer algo especial para los retries.
            List<ProcessError> errorList = GetErrorsFromFile(userConfiguration, fileName);
            List<ProcessResult> resultList = GetResultsFromFile(userConfiguration, fileName);

            IQueueProducer producer = GetProducer();

            //TODO sacar threads count from userConfiguration.
            List<IQueueConsumer> consumers = GetConsumers(userConfiguration.MaxThreadsNumber);

            var consumerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken consumerCancellationToken = consumerCancellationTokenSource.Token;

            var statusCancellationTokenSource = new CancellationTokenSource();
            CancellationToken statusCancellationToken = statusCancellationTokenSource.Token;

            Task taskProducer = Task.Factory.StartNew(() => producer.GetMessages(userConfiguration, outboundQueue, errorList, resultList, fileName, consumerCancellationToken));

            //descomentar para probar el productor
            //taskProducer.Wait();

            var tasks = new List<Task>();

            foreach (IQueueConsumer queueConsumer in consumers)
            {
                Task taskConsumer = Task.Factory.StartNew(() => queueConsumer.ProcessMessages(userConfiguration, outboundQueue, consumerCancellationToken));

                tasks.Add(taskConsumer);
            }

            if (userConfiguration.Status != null)
            {
                Task taskStatus = Task.Factory.StartNew(() => UpdateStatusFile(fileName, userConfiguration, statusCancellationToken));
            }

            try
            {
                taskProducer.Wait();
            }
            catch (Exception e)
            {
                _logger.Error($"PROCESS FILE ERROR: {e}");
            }
            finally
            {
                //espero que se vacie la lista y aviso con el cancel token a los consumidores.
                while (outboundQueue.GetCount() > 0)
                {
                    Thread.Sleep(WAITING_CONSUMERS_TIME);
                }

                consumerCancellationTokenSource.Cancel();

                Task.WaitAll(tasks.ToArray());

                statusCancellationTokenSource.Cancel();
            }
        }

        protected abstract IQueueProducer GetProducer();

        protected abstract List<IQueueConsumer> GetConsumers(int count);

        protected void Processor_ErrorEvent(object sender, QueueErrorEventArgs e)
        {
            var processError = new ProcessError()
            {
                LineNumber = e.LineNumber,
                Date = e.Date,
                Message = e.Message,
                Type = e.Type,
                Description = e.Description
            };

            string text = JsonConvert.SerializeObject(processError, Formatting.None);

            errorFileWriter.AppendLine(text);

            lock (_lockProcessed)
            {
                _processed++;
                _errors++;
            }
        }

        protected void Processor_ResultEvent(object sender, QueueResultEventArgs e)
        {
            var processResult = new ProcessResult()
            {
                LineNumber = e.LineNumber,
                ResourceId = e.ResourceId,
                DeliveryLink = e.DeliveryLink,
                Message = e.Message,
                EnqueueTime = e.EnqueueTime,
                DequeueTime = e.DequeueTime,
                DeliveryTime = e.DeliveryTime
            };

            string text = JsonConvert.SerializeObject(processResult, Formatting.None);

            resultFileWriter.AppendLine(text);

            lock (_lockProcessed)
            {
                _processed++;
            }
        }

        private bool ValidateCredentials(CredentialsConfiguration credentials)
        {
            var restClient = new RestClient(_configuration.BaseUrl);

            string resource = _configuration.AccountUrl.Replace("{AccountId}", credentials.AccountId.ToString());
            var request = new RestRequest(resource, Method.GET);

            string value = $"token {credentials.ApiKey}";
            request.AddHeader("Authorization", value);

            try
            {
                IRestResponse response = restClient.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    string result = response.Content;
                    _logger.Info($"Validate credentials fail:{result}");
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Validate credentials error -- {e}");
                return false;
            }
        }
    }
}
