﻿using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Relay.BulkSenderService.Reports
{
    public class RicohStatusReportProcessor : DailyReportProcessor
    {
        public RicohStatusReportProcessor(ILog logger, IConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {
        }

        protected override List<string> GetFilesToProcess(IUserConfiguration user, ReportExecution reportExecution)
        {
            return new List<string>();
        }

        public override bool GenerateForcedReport(List<string> files, IUserConfiguration user, ReportExecution reportExecution)
        {
            _logger.Debug($"Process forced status report for user {user.Name}.");

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new SplitCsvReport()
            {
                Separator = _reportTypeConfiguration.FieldSeparator,
                ReportPath = filePathHelper.GetForcedReportsFolder(),
                ReportName = _reportTypeConfiguration.Name.GetReportName(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            report.AddHeaders(GetHeadersList(_reportTypeConfiguration.ReportFields));

            List<ReportItem> items = GetReportItems("", ' ', user.Credentials.AccountId, user.UserGMT, _reportTypeConfiguration.DateFormat, reportExecution.LastRun, reportExecution.NextRun);

            report.AppendItems(items);

            report.SplitGenerate();

            return true;
        }

        protected override void ProcessFilesForReports(List<string> files, IUserConfiguration user, ReportExecution reportExecution)
        {
            _logger.Debug($"Process Ricoh Status report for user {user.Name}.");

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

            List<ReportItem> items = GetReportItems("", ' ', user.Credentials.AccountId, user.UserGMT, _reportTypeConfiguration.DateFormat, reportExecution.LastRun, reportExecution.NextRun);

            report.AppendItems(items);

            List<string> reports = report.SplitGenerate();

            foreach (string reportFileName in reports)
            {
                if (File.Exists(reportFileName))
                {
                    var ftpHelper = user.Ftp.GetFtpHelper(_logger);

                    UploadFileToFtp(reportFileName, ((UserApiConfiguration)user).Reports.Folder, ftpHelper);
                }
            }
        }

        protected List<ReportItem> GetReportItems(string file, char separator, int userId, int reportGMT, string dateFormat, DateTime start, DateTime end)
        {
            var items = new List<ReportItem>();

            DateTime startTime = end.AddHours(-_reportTypeConfiguration.OffsetHour);

            try
            {
                GetDataFromDB(items, dateFormat, userId, reportGMT, startTime, end);

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

            try
            {
                List<DBStatusDto> dbReportItemList = sqlHelper.GetResultsByDeliveryDate(userId, start, end);

                foreach (DBStatusDto dbReportItem in dbReportItemList)
                {
                    var item = new ReportItem(_reportTypeConfiguration.ReportFields.Count);

                    MapDBStatusDtoToReportItem(dbReportItem, item, reportGMT, dateFormat);

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
    }
}