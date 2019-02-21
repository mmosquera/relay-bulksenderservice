using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace Relay.BulkSenderService.Reports
{
    public class BanorteResumeReportProcessor : DailyReportProcessor
    {
        public BanorteResumeReportProcessor(ILog logger, IConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration)
            : base(logger, configuration, reportTypeConfiguration)
        {

        }

        protected override List<string> ProcessFilesForReports(List<string> files, IUserConfiguration user, ReportExecution reportExecution)
        {
            if (files.Count == 0)
            {
                return null;
            }

            _logger.Debug($"Create resume report for user {user.Name}.");

            //var ftpHelper = user.Ftp.GetFtpHelper(_logger);
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new TemplateReport()
            {
                ReportName = _reportTypeConfiguration.Name.GetReportName(),
                ReportPath = filePathHelper.GetReportsFilesFolder(),
            };

            foreach (string file in files)
            {
                ITemplateConfiguration template = ((UserApiConfiguration)user).GetTemplateConfiguration(file);

                List<ReportItem> items = GetReportItems(file, template.FieldSeparator, user.Credentials.AccountId, user.UserGMT, "dd/MM/yyyy HH:mm");

                report.AppendItems(items);
            }

            string reportFileName = report.Generate();

            var reports = new List<string>();

            if (File.Exists(reportFileName))
            {
                reports.Add(reportFileName);

                reportExecution.ReportFile = Path.GetFileName(reportFileName);

                SendEmail(user, reportFileName);
            }

            return reports;
        }

        private void SendEmail(IUserConfiguration user, string reportFileName)
        {
            if (user.AdminEmail != null && user.AdminEmail.Emails.Count > 0)
            {
                var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
                smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

                var mailMessage = new MailMessage();
                mailMessage.Subject = "Doppler Relay - Resumen Importacion";
                mailMessage.From = new MailAddress("support@dopplerrelay.com", "Doppler Relay Support");
                foreach (string email in user.AdminEmail.Emails)
                {
                    mailMessage.To.Add(email);
                }

                mailMessage.Body = File.ReadAllText(reportFileName);
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

        protected override List<ReportItem> GetReportItems(string file, char separator, int userId, int reportGMT, string dateFormat)
        {
            var items = new List<ReportItem>();
            int processed, errors;
            processed = errors = 0;

            try
            {
                using (var streamReader = new StreamReader(file))
                {
                    List<string> fileHeaders = streamReader.ReadLine().Split(separator).ToList();

                    List<ReportFieldConfiguration> reportHeaders = GetHeadersIndexes(_reportTypeConfiguration.ReportFields, fileHeaders, out int processedIndex, out int resultIndex);

                    if (processedIndex == -1 || resultIndex == -1)
                    {
                        return items;
                    }

                    while (!streamReader.EndOfStream)
                    {
                        string[] lineArray = streamReader.ReadLine().Split(separator);

                        processed++;

                        if (lineArray.Length <= resultIndex || lineArray[processedIndex] != Constants.PROCESS_RESULT_OK)
                        {
                            errors++;
                        }
                    }
                }

                var fileInfo = new FileInfo(file);

                var reportItem = new ReportItem(4);

                reportItem.AddValue(fileInfo.Name, 0);
                reportItem.AddValue(fileInfo.LastWriteTimeUtc.ToString(), 1);
                reportItem.AddValue(processed.ToString(), 2);
                reportItem.AddValue(errors.ToString(), 3);

                items.Add(reportItem);

                return items;
            }
            catch (Exception)
            {
                _logger.Error("Error trying to get report items");
                throw;
            }
        }
    }
}
