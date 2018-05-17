using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Reports
{
    public class EdesurReportProcessor : ReportProcessor
    {
        public EdesurReportProcessor(IConfiguration configuration, ILog logger, ReportTypeConfiguration reportTypeConfiguration)
            : base(logger, configuration, reportTypeConfiguration)
        {

        }

        protected override List<string> GetFilesToProcess(IUserConfiguration user)
        {
            DateTime nextRun = GetNextRun(user.Name);

            if (nextRun > DateTime.UtcNow)
            {
                return new List<string>();
            }

            var filePathHelper = new FilePathHelper(_configuration, user.Name);
            var directoryInfo = new DirectoryInfo(filePathHelper.GetResultsFilesFolder());

            DateTime date = DateTime.UtcNow.AddDays(-21);

            var fileInfoList = directoryInfo.GetFiles("*.report").Concat(directoryInfo.GetFiles("*.sent"))
                .Where(f => f.CreationTimeUtc > date)
                .OrderBy(f => f.CreationTime);

            return FilterFilesByTemplate(fileInfoList.Select(x => x.FullName).ToList(), user);
        }

        private DateTime GetNextRun(string userName)
        {
            var filePathHelper = new FilePathHelper(_configuration, userName);
            var directoryInfo = new DirectoryInfo(filePathHelper.GetReportsFilesFolder());

            FileInfo fileInfo = directoryInfo.GetFiles("*EDESUR*").OrderByDescending(x => x.CreationTime).FirstOrDefault();

            if (fileInfo != null)
            {
                return fileInfo.CreationTimeUtc.AddHours(3);
            }

            return DateTime.UtcNow;
        }

        protected override void ProcessFilesForReports(List<string> files, IUserConfiguration user)
        {
            if (files.Count == 0)
            {
                return;
            }

            _logger.Debug($"Crete Edesur report for user {user.Name}.");

            var ftpHelper = user.Ftp.GetFtpHelper(_logger);
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new EdesurReport(_logger, _reportTypeConfiguration);
            report.SourceFiles = files;
            report.Separator = _reportTypeConfiguration.FieldSeparator;
            report.ReportPath = filePathHelper.GetReportsFilesFolder();
            report.ReportGMT = user.UserGMT;
            report.UserId = user.Credentials.AccountId;

            string reportFileName = report.Generate();

            if (File.Exists(reportFileName))
            {
                UploadFileToFtp(reportFileName, ((UserApiConfiguration)user).Reports.Folder, ftpHelper);
            }
        }

        public override bool GenerateForcedReport(List<string> files, IUserConfiguration user)
        {
            List<string> filteredFiles = FilterFilesByTemplate(files, user);

            if (filteredFiles.Count == 0)
            {
                return false;
            }

            _logger.Debug($"Crete Edesur report for user {user.Name}.");

            var ftpHelper = user.Ftp.GetFtpHelper(_logger);
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new EdesurReport(_logger, _reportTypeConfiguration)
            {
                SourceFiles = files,
                Separator = _reportTypeConfiguration.FieldSeparator,
                ReportPath = filePathHelper.GetForcedReportsFolder(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            report.Generate();

            return true;
        }
    }
}
