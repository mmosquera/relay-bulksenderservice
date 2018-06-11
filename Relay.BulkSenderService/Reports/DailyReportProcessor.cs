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

            var report = new CsvReport(_logger)
            {
                SourceFiles = files,
                Separator = _reportTypeConfiguration.FieldSeparator,
                ReportPath = filePathHelper.GetReportsFilesFolder(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            report.AddHeaders(GetHeadersList(_reportTypeConfiguration.ReportFields, null));

            foreach (string file in files)
            {
                ITemplateConfiguration template = ((UserApiConfiguration)user).GetTemplateConfiguration(file);

                List<ReportItem> items = GetReportItems(file, template.FieldSeparator, user.Credentials.AccountId, user.UserGMT, "dd/MM/yyyy HH:mm");

                report.AppendItems(items);
            }

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

            var report = new CsvReport(_logger)
            {
                SourceFiles = filteredFiles,
                // TODO: get from configuration template.
                Separator = ',',
                ReportPath = filePathHelper.GetForcedReportsFolder(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            report.Generate();

            return true;
        }

        protected List<ReportItem> GetReportItems(string file, char separator, int userId, int reportGMT, string dateFormat)
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

        protected override bool IsTimeToRun(IUserConfiguration user)
        {
            return true;
        }
    }
}
