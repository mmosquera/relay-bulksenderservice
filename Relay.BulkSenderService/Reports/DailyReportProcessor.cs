using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Reports
{
    public class DailyReportProcessor : ReportProcessor
    {
        public DailyReportProcessor(IConfiguration configuration, ILog logger, ReportTypeConfiguration reportTypeConfiguration)
            : base(logger, configuration, reportTypeConfiguration)
        {

        }

        protected override List<string> GetFilesToProcess(IUserConfiguration user)
        {
            DateTime now = DateTime.UtcNow.AddHours(user.UserGMT);

            if (!IsTimeToExecute(user.Name, now))
            {
                return new List<string>();
            }

            var filePathHelper = new FilePathHelper(_configuration, user.Name);
            var directoryInfo = new DirectoryInfo(filePathHelper.GetResultsFilesFolder());

            DateTime end = new DateTime(now.Year, now.Month, now.Day);
            DateTime start = end.AddDays(-1);

            var fileInfoList = directoryInfo.GetFiles("*.sent")
                .Where(f => f.LastWriteTimeUtc >= start && f.LastWriteTimeUtc < end)
                .OrderBy(f => f.CreationTimeUtc);

            return FilterFilesByTemplate(fileInfoList.Select(x => x.FullName).ToList(), user);

        }

        public bool IsTimeToExecute(string userName, DateTime now)
        {
            string fixPart = _reportTypeConfiguration.Name.Parts.OfType<FixReportNamePart>().Select(x => x.Value).FirstOrDefault();

            var filePathHelper = new FilePathHelper(_configuration, userName);

            var directoryInfo = new DirectoryInfo(filePathHelper.GetReportsFilesFolder());

            FileInfo lastReport = directoryInfo.GetFiles().Where(x => x.Name.Contains(fixPart)).OrderByDescending(x => x.CreationTime).FirstOrDefault();

            DateTime date = new DateTime(now.Year, now.Month, now.Day, _reportTypeConfiguration.Hour, 0, 0);

            if (date <= now && (lastReport == null || DateTime.UtcNow.Subtract(lastReport.CreationTimeUtc).TotalHours >= 24))
            {
                return true;
            }

            return false;
        }

        protected override void ProcessFilesForReports(List<string> files, IUserConfiguration user)
        {
            if (files.Count == 0)
            {
                return;
            }

            _logger.Debug($"Create daily report for user {user.Name}.");

            var ftpHelper = user.Ftp.GetFtpHelper(_logger);
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new CsvReport(_logger, _reportTypeConfiguration)
            {
                SourceFiles = files,
                // TODO: get from configuration. 
                //Separator = _reportTypeConfiguration.FieldSeparator,
                Separator = ',',
                ReportPath = filePathHelper.GetReportsFilesFolder(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            string reportFileName = report.Generate();

            if (File.Exists(reportFileName))
            {
                UploadFileToFtp(reportFileName, ((UserApiConfiguration)user).Reports.Folder, ftpHelper);

                foreach (string file in files)
                {
                    string renameFile = file.Replace(".sent", ".report");
                    File.Move(file, renameFile);
                }
            }
        }

        public override bool GenerateForcedReport(List<string> files, IUserConfiguration user)
        {
            List<string> filteredFiles = FilterFilesByTemplate(files, user);

            if (filteredFiles.Count == 0)
            {
                return false;
            }

            _logger.Debug($"Create daily report for user {user.Name}.");

            var ftpHelper = user.Ftp.GetFtpHelper(_logger);
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new CsvReport(_logger, _reportTypeConfiguration)
            {
                SourceFiles = filteredFiles,
                //Separator = _reportTypeConfiguration.FieldSeparator,
                Separator = ',',
                ReportPath = filePathHelper.GetForcedReportsFolder(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            report.Generate();

            return true;
        }
    }
}
