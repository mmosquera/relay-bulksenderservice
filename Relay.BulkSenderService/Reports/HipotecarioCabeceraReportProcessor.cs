using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.BulkSenderService.Reports
{
    public class HipotecarioCabeceraReportProcessor : HipotecarioReportProcessor
    {
        public HipotecarioCabeceraReportProcessor(ILog logger, IConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {
        }

        public override bool GenerateForcedReport(List<string> files, IUserConfiguration user)
        {
            return true;
        }

        protected override List<string> GetFilesToProcess(IUserConfiguration user)
        {
            return new List<string>();
        }

        protected override void ProcessFilesForReports(List<string> files, IUserConfiguration user)
        {
            _logger.Debug($"Process cabecera report for user {user.Name}.");

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new ZipCsvReport(_logger)
            {
                Separator = _reportTypeConfiguration.FieldSeparator,
                ReportPath = filePathHelper.GetReportsFilesFolder(),
                ReportName = _reportTypeConfiguration.Name.GetReportName(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            report.AddHeaders(GetHeadersList(_reportTypeConfiguration.ReportFields));

            List<ReportItem> items = GetReportItems("", ' ', user.Credentials.AccountId, user.UserGMT, "dd/MM/yyyy HH:mm");

            report.AppendItems(items);

            string reportFileName = report.Generate();
        }

        protected override List<ReportItem> GetReportItems(string file, char separator, int userId, int reportGMT, string dateFormat)
        {
            var items = new List<ReportItem>();

            try
            {
                GetDataFromDB(items, dateFormat, userId, reportGMT);

                return items;
            }
            catch (Exception)
            {
                _logger.Error("Error trying to get report items");
                throw;
            }
        }

        protected override void GetDataFromDB(List<ReportItem> items, string dateFormat, int userId, int reportGMT)
        {
            var sqlHelper = new SqlHelper();

            try
            {
                DateTime end = DateTime.UtcNow;
                DateTime start = end.AddHours(-24);

                List<DBSummarizedReportItem> dbReportItemList = sqlHelper.GetSummarizedByDate(userId, start, end);

                foreach (DBSummarizedReportItem dbReportItem in dbReportItemList)
                {
                    var item = new ReportItem(_reportTypeConfiguration.ReportFields.Count);

                    MapDBDataToReportItem(dbReportItem, item, reportGMT, dateFormat);

                    items.Add(item);
                }

                sqlHelper.CloseConnection();
            }
            catch (Exception e)
            {
                _logger.Error($"Error on get data from DB {e}");
                throw;
            }
        }

        private void MapDBDataToReportItem(DBSummarizedReportItem dbReportItem, ReportItem item, int reportGMT = 0, string dateFormat = "")
        {
            foreach (ReportFieldConfiguration reportField in _reportTypeConfiguration.ReportFields.Where(x => !string.IsNullOrEmpty(x.NameInDB)))
            {
                switch (reportField.NameInDB)
                {
                    case "TemplateId":
                        item.AddValue(dbReportItem.TemplateId.ToString(), reportField.Position);
                        break;
                    case "TemplateName":
                        item.AddValue(dbReportItem.TemplateName, reportField.Position);
                        break;
                    case "TemplateGuid":
                        item.AddValue(dbReportItem.TemplateGuid, reportField.Position);
                        break;
                    case "TemplateFromEmail":
                        item.AddValue(dbReportItem.TemplateFromEmail, reportField.Position);
                        break;
                    case "TemplateFromName":
                        item.AddValue(dbReportItem.TemplateFromName, reportField.Position);
                        break;
                    case "TemplateSubject":
                        item.AddValue(dbReportItem.TemplateSubject, reportField.Position);
                        break;
                    case "TotalDeliveries":
                        item.AddValue(dbReportItem.TotalDeliveries.ToString(), reportField.Position);
                        break;
                    case "TotalRetries":
                        item.AddValue(dbReportItem.TotalRetries.ToString(), reportField.Position);
                        break;
                    case "TotalOpens":
                        item.AddValue(dbReportItem.TotalOpens.ToString(), reportField.Position);
                        break;
                    case "TotalUniqueOpens":
                        item.AddValue(dbReportItem.TotalUniqueOpens.ToString(), reportField.Position);
                        break;
                    case "LastOpenDate":
                        item.AddValue(dbReportItem.LastOpenDate.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        break;
                    case "TotalClicks":
                        item.AddValue(dbReportItem.TotalClicks.ToString(), reportField.Position);
                        break;
                    case "TotalUniqueClicks":
                        item.AddValue(dbReportItem.TotalUniqueClicks.ToString(), reportField.Position);
                        break;
                    case "LastClickDate":
                        item.AddValue(dbReportItem.LastClickDate.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        break;
                    case "TotalUnsubscriptions":
                        item.AddValue(dbReportItem.TotalUnsubscriptions.ToString(), reportField.Position);
                        break;
                    case "TotalHardBounces":
                        item.AddValue(dbReportItem.TotalHardBounces.ToString(), reportField.Position);
                        break;
                    case "TotalSoftBounces":
                        item.AddValue(dbReportItem.TotalSoftBounces.ToString(), reportField.Position);
                        break;
                }
            }
        }
    }
}
