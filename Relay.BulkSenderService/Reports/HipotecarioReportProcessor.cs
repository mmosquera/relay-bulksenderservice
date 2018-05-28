using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Reports
{
    public abstract class HipotecarioReportProcessor : ReportProcessor
    {
        public HipotecarioReportProcessor(ILog logger, IConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {
        }

        protected override List<string> GetFilesToProcess(IUserConfiguration user)
        {
            DateTime end = DateTime.UtcNow;
            DateTime start = new DateTime(end.Year, end.Month, 25, 0, 0, 0);

            var filePathHelper = new FilePathHelper(_configuration, user.Name);
            var directoryInfo = new DirectoryInfo(filePathHelper.GetResultsFilesFolder());

            var fileInfoList = directoryInfo.GetFiles("*.sent").Concat(directoryInfo.GetFiles("*.report"))
                .Where(f => f.CreationTimeUtc >= start && f.CreationTimeUtc < end)
                .OrderBy(f => f.CreationTime);

            return FilterFilesByTemplate(fileInfoList.Select(x => x.FullName).ToList(), user);
        }

        protected override bool IsTimeToRun(IUserConfiguration user)
        {
            var filePathHelper = new FilePathHelper(_configuration, user.Name);
            var directoryInfo = new DirectoryInfo(filePathHelper.GetReportsFilesFolder());

            string filter = $"*.{_reportTypeConfiguration.Name.Extension}";

            FileInfo report = directoryInfo
                .GetFiles(filter)
                .Where(x => _reportTypeConfiguration.Name.Parts.OfType<FixReportNamePart>().All(y => x.Name.Contains(y.GetValue())))
                .OrderByDescending(x => x.CreationTimeUtc)
                .FirstOrDefault();

            return (_reportTypeConfiguration.Hour >= DateTime.UtcNow.Hour
                && (report == null || DateTime.UtcNow.Subtract(report.LastWriteTimeUtc).TotalHours >= 24));
        }
    }
}