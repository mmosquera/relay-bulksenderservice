using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System.IO;

namespace Relay.BulkSenderService.Reports
{
    public class CsvFileReportProcessor : FileReportProcessor
    {
        public CsvFileReportProcessor(IConfiguration configuration, ILog logger, ReportTypeConfiguration reportTypeConfiguration)
            : base(configuration, logger, reportTypeConfiguration)
        {

        }

        protected override ReportBase GetReport(string file, FilePathHelper filePathHelper, IUserConfiguration user)
        {
            var report = new CsvReport()
            {
                ReportName = _reportTypeConfiguration.Name.GetReportName(Path.GetFileName(file), filePathHelper.GetReportsFilesFolder()),
                ReportPath = filePathHelper.GetReportsFilesFolder(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId,
                Separator = _reportTypeConfiguration.FieldSeparator
            };

            return report;
        }
    }
}
