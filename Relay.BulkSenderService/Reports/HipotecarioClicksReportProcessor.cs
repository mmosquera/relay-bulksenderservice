using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.BulkSenderService.Reports
{
    public class HipotecarioClicksReportProcessor : HipotecarioReportProcessor
    {
        public HipotecarioClicksReportProcessor(ILog logger, IConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {
        }

        public override bool GenerateForcedReport(List<string> files, IUserConfiguration user)
        {
            return true;
        }        

        protected override void GetDataFromDB(List<ReportItem> items, string dateFormat, int userId, int reportGMT)
        {
            List<string> guids = items.Select(it => it.ResultId).Distinct().ToList();

            var sqlHelper = new SqlHelper();

            try
            {
                int i = 0;
                while (i < guids.Count)
                {
                    // TODO use skip take from linq.
                    var aux = new List<string>();
                    for (int count = 0; i < guids.Count && count < 1000; count++)
                    {
                        aux.Add(guids[i]);
                        i++;
                    }

                    List<DBStatusReportItem> dbReportItemList = sqlHelper.GetClicksByDeliveryList(userId, aux);
                    foreach (DBStatusReportItem dbReportItem in dbReportItemList)
                    {
                        ReportItem item = items.FirstOrDefault(x => x.ResultId == dbReportItem.MessageGuid);
                        if (item != null)
                        {
                            MapDBDataToReportItem(dbReportItem, item, reportGMT, dateFormat);
                        }
                    }

                    aux.Clear();
                }

                sqlHelper.CloseConnection();
            }
            catch (Exception e)
            {
                _logger.Error($"Error on get data from DB {e}");
                throw;
            }
        }

        private void MapDBDataToReportItem(DBStatusReportItem dbReportItem, ReportItem item, int reportGMT = 0, string dateFormat = "")
        {
            string status;
            string description;
            GetStatusAndDescription(dbReportItem, out status, out description);

            foreach (ReportFieldConfiguration reportField in _reportTypeConfiguration.ReportFields.Where(x => !string.IsNullOrEmpty(x.NameInDB)))
            {
                switch (reportField.NameInDB)
                {
                    case "CreatedAt":
                        item.AddValue(dbReportItem.CreatedAt.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        break;
                    case "Status":
                        item.AddValue(status, reportField.Position);
                        break;
                    case "Description":
                        item.AddValue(description, reportField.Position);
                        break;
                    case "ClickEventsCount":
                        item.AddValue(dbReportItem.ClickEventsCount.ToString(), reportField.Position);
                        break;
                    case "OpenEventsCount":
                        item.AddValue(dbReportItem.OpenEventsCount.ToString(), reportField.Position);
                        break;
                    case "SentAt":
                        item.AddValue(dbReportItem.SentAt.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        break;
                    case "Subject":
                        item.AddValue(dbReportItem.Subject, reportField.Position);
                        break;
                    case "FromEmail":
                        item.AddValue(dbReportItem.FromEmail, reportField.Position);
                        break;
                    case "FromName":
                        item.AddValue(dbReportItem.FromName, reportField.Position);
                        break;
                    case "Address":
                        item.AddValue(dbReportItem.Address, reportField.Position);
                        break;
                    case "OpenDate":
                        item.AddValue(dbReportItem.OpenDate.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        break;
                    case "ClickDate":
                        item.AddValue(dbReportItem.ClickDate.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        break;
                    case "BounceDate":
                        item.AddValue(dbReportItem.BounceDate.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        break;
                    case "LinkUrl":
                        item.AddValue(dbReportItem.LinkUrl, reportField.Position);
                        break;
                    case "Unsubscribed":
                        item.AddValue(dbReportItem.Unsubscribed.ToString(), reportField.Position);
                        break;
                }
            }
        }
    }
}
