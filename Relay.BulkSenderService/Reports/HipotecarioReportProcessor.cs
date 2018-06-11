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
            DateTime start = GetLastTimeReport(user.Name);

            if (start == DateTime.MinValue)
            {
                start = end.AddHours(-24);
            }

            var filePathHelper = new FilePathHelper(_configuration, user.Name);
            var directoryInfo = new DirectoryInfo(filePathHelper.GetResultsFilesFolder());

            var fileInfoList = directoryInfo.GetFiles("*.sent").Concat(directoryInfo.GetFiles("*.report"))
                .Where(f => f.CreationTimeUtc >= start && f.CreationTimeUtc < end)
                .OrderBy(f => f.CreationTime);

            return FilterFilesByTemplate(fileInfoList.Select(x => x.FullName).ToList(), user);
        }

        protected override void ProcessFilesForReports(List<string> files, IUserConfiguration user)
        {
            if (files.Count == 0)
            {
                return;
            }

            _logger.Debug($"Create Detail Report for user {user.Name}.");

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

            foreach (string file in files)
            {
                ITemplateConfiguration template = ((UserApiConfiguration)user).GetTemplateConfiguration(file);

                List<ReportItem> items = GetReportItems(file, template.FieldSeparator, user.Credentials.AccountId, user.UserGMT, "dd/MM/yyyy HH:mm");

                report.AppendItems(items);
            }

            string reportFileName = report.Generate();

            if (File.Exists(reportFileName))
            {
                var ftpHelper = user.Ftp.GetFtpHelper(_logger);

                UploadFileToFtp(reportFileName, ((UserApiConfiguration)user).Reports.Folder, ftpHelper);

                foreach (string file in files)
                {
                    string renameFile = file.Replace(".sent", ".report");
                    File.Move(file, renameFile);
                }
            }
        }

        protected override bool IsTimeToRun(IUserConfiguration user)
        {
            DateTime now = DateTime.UtcNow.AddHours(user.UserGMT);
            DateTime reportTime = new DateTime(now.Year, now.Month, now.Day, _reportTypeConfiguration.Hour, 0, 0);

            if (reportTime > now)
            {
                return false;
            }

            DateTime lastReportTime = GetLastTimeReport(user.Name);

            if (lastReportTime == DateTime.MinValue)
            {
                return true;
            }

            return reportTime.Subtract(lastReportTime.AddHours(user.UserGMT)).TotalHours > 23;
        }

        private DateTime GetLastTimeReport(string name)
        {
            var filePathHelper = new FilePathHelper(_configuration, name);
            var directoryInfo = new DirectoryInfo(filePathHelper.GetReportsFilesFolder());

            string filter = $"*.{_reportTypeConfiguration.Name.Extension}";

            FileInfo report = directoryInfo
                .GetFiles(filter)
                .Where(x => _reportTypeConfiguration.Name.Parts.OfType<FixReportNamePart>().All(y => x.Name.Contains(y.GetValue())))
                .OrderByDescending(x => x.CreationTimeUtc)
                .FirstOrDefault();

            DateTime lastReportTime = report != null ? report.LastWriteTimeUtc : DateTime.MinValue;

            return lastReportTime;
        }

        protected virtual List<ReportItem> GetReportItems(string file, char separator, int userId, int reportGMT, string dateFormat)
        {
            var items = new List<ReportItem>();

            try
            {
                using (var streamReader = new StreamReader(file))
                {
                    List<string> fileHeaders = streamReader.ReadLine().Split(separator).ToList();

                    // Contiene el header(key) y la posicion(value) en el archivo original, que seran incluidos en el reporte.
                    Dictionary<string, int> headers = GetHeadersIndexes(_reportTypeConfiguration.ReportFields, fileHeaders, out int processedIndex, out int resultIndex);

                    if (processedIndex == -1 || resultIndex == -1)
                    {
                        return items;
                    }

                    while (!streamReader.EndOfStream)
                    {
                        string[] lineArray = streamReader.ReadLine().Split(separator);

                        if (lineArray.Length <= resultIndex || lineArray[processedIndex] != Constants.PROCESS_RESULT_OK)
                        {
                            continue;
                        }

                        var item = new ReportItem(_reportTypeConfiguration.ReportFields.Count);

                        foreach (string key in headers.Keys)
                        {
                            //index in original file.
                            int value = headers[key];

                            //index in report
                            int index = value;
                            ReportFieldConfiguration field = _reportTypeConfiguration.ReportFields.FirstOrDefault(x => x.NameInFile == key);
                            if (field != null)
                            {
                                index = field.Position;
                            }

                            item.AddValue(lineArray[value].Trim(), index);
                        }

                        item.ResultId = lineArray[resultIndex];

                        items.Add(item);
                    }
                }

                GetDataFromDB(items, dateFormat, userId, reportGMT);

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