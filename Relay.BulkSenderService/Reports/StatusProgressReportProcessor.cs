using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Relay.BulkSenderService.Reports
{
    public class StatusProgressReportProcessor : ReportProcessor
    {
        private const int MAX_HOUR_RANGE = 6;
        private Dictionary<int, DBStatusDto> tempReport;

        public StatusProgressReportProcessor(ILog logger, IConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {

        }

        protected override List<string> GetFilesToProcess(IUserConfiguration user, ReportExecution reportExecution)
        {
            return new List<string>();
        }

        protected override List<string> ProcessFilesForReports(List<string> files, IUserConfiguration user, ReportExecution reportExecution)
        {
            _logger.Debug($"Process status progress report for user {user.Name}.");

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new SplitCsvReport()
            {
                Separator = _reportTypeConfiguration.FieldSeparator,
                ReportPath = filePathHelper.GetReportsFilesFolder(),
                ReportName = _reportTypeConfiguration.Name.GetReportName(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            report.AddHeaders(GetHeadersList(_reportTypeConfiguration.ReportFields));

            DateTime start = reportExecution.LastRun;

            if (tempReport != null)
            {
                tempReport = LoadTempReport(user.Name);
            }
            else
            {
                start = reportExecution.NextRun.AddHours(-_reportTypeConfiguration.OffsetHour);
                tempReport = new Dictionary<int, DBStatusDto>();
            }

            List<ReportItem> items = GetReportItems("", ' ', user.Credentials.AccountId, user.UserGMT, _reportTypeConfiguration.DateFormat, start, reportExecution.NextRun);

            SaveTempReport(user.Name);

            report.AppendItems(items);

            List<string> reports = report.SplitGenerate();

            foreach (string reportFileName in reports)
            {
                if (File.Exists(reportFileName))
                {
                    reportExecution.ReportFile = string.Join("|", reports.Select(x => Path.GetFileName(x)));

                    var ftpHelper = user.Ftp.GetFtpHelper(_logger);

                    UploadFileToFtp(reportFileName, ((UserApiConfiguration)user).Reports.Folder, ftpHelper);
                }
            }

            return reports;
        }

        protected List<ReportItem> GetReportItems(string file, char separator, int userId, int reportGMT, string dateFormat, DateTime start, DateTime end)
        {
            var items = new List<ReportItem>();

            try
            {
                GetDataFromDB(items, dateFormat, userId, reportGMT, start, end);

                start = end.AddHours(-_reportTypeConfiguration.OffsetHour);
                var keysToRemove = new List<int>();

                foreach (int key in tempReport.Keys)
                {
                    DBStatusDto dbReportItem = tempReport[key];

                    if (dbReportItem.CreatedAt >= start)
                    {
                        var item = new ReportItem(_reportTypeConfiguration.ReportFields.Count);

                        MapDBStatusDtoToReportItem(dbReportItem, item, reportGMT, dateFormat);

                        items.Add(item);
                    }
                    else
                    {
                        keysToRemove.Add(key);
                    }
                }

                ClearTempReport(keysToRemove);

                return items;
            }
            catch (Exception)
            {
                _logger.Error("Error trying to get report items");
                throw;
            }
        }

        protected void GetDataFromDB(List<ReportItem> items, string dateFormat, int userId, int reportGMT, DateTime start, DateTime end)
        {
            var sqlHelper = new SqlHelper();

            DateTime from, to;
            from = start;
            to = start.AddHours(MAX_HOUR_RANGE);

            try
            {
                while (from < to)
                {
                    List<DBStatusDto> dbReportItemList = sqlHelper.GetResultsByDeliveryDate(userId, from, to);

                    foreach (DBStatusDto statusDto in dbReportItemList)
                    {
                        if (tempReport.ContainsKey(statusDto.DeliveryId))
                        {
                            tempReport[statusDto.DeliveryId] = statusDto;
                        }
                        else
                        {
                            tempReport.Add(statusDto.DeliveryId, statusDto);
                        }
                    }

                    from = to;
                    to = to.AddHours(MAX_HOUR_RANGE);
                    if (to > end)
                    {
                        to = end;
                    }
                }

                sqlHelper.CloseConnection();
            }
            catch (Exception e)
            {
                _logger.Error($"Error on get data from DB {e}");
                throw;
            }
        }

        private Dictionary<int, DBStatusDto> LoadTempReport(string userName)
        {
            var filePathHelper = new FilePathHelper(_configuration, userName);

            string fileName = $@"{filePathHelper.GetReportsFilesFolder()}\report.{_reportTypeConfiguration.ReportId}.tmp";

            if (File.Exists(fileName))
            {
                try
                {
                    using (FileStream fileStream = File.OpenRead(fileName))
                    {
                        return new BinaryFormatter().Deserialize(fileStream) as Dictionary<int, DBStatusDto>;
                    }
                }
                catch (Exception e)
                {
                    //si esta dañado lo borro y se genera de nuevo.
                    File.Delete(fileName);
                }
            }

            return null;
        }

        private void SaveTempReport(string userName)
        {
            var filePathHelper = new FilePathHelper(_configuration, userName);

            string fileName = $@"{filePathHelper.GetReportsFilesFolder()}\report.{_reportTypeConfiguration.ReportId}.tmp";

            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                new BinaryFormatter().Serialize(fileStream, tempReport);
            }
        }

        private void ClearTempReport(List<int> keysToRemove)
        {
            foreach (int key in keysToRemove)
            {
                tempReport.Remove(key);
            }
        }
    }
}