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

            //var report = new EdesurReport(_logger, _reportTypeConfiguration);
            //report.SourceFiles = files;
            //report.Separator = _reportTypeConfiguration.FieldSeparator;
            //report.ReportPath = filePathHelper.GetReportsFilesFolder();
            //report.ReportGMT = user.UserGMT;
            //report.UserId = user.Credentials.AccountId;
            var report = new CsvReport(_logger)
            {
                ReportGMT = user.UserGMT,
                ReportName = _reportTypeConfiguration.Name.GetReportName(),
                ReportPath = filePathHelper.GetReportsFilesFolder(),
                Separator = _reportTypeConfiguration.FieldSeparator,
                UserId = user.Credentials.AccountId
            };

            foreach (string file in files)
            {
                ITemplateConfiguration template = ((UserApiConfiguration)user).GetTemplateConfiguration(file);
                List<ReportItem> items = GetReportItems(file, template.FieldSeparator, user.Credentials.AccountId, user.UserGMT);
                report.AppendItems(items);
            }

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
                //Separator = _reportTypeConfiguration.FieldSeparator,
                ReportPath = filePathHelper.GetForcedReportsFolder(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            report.Generate();

            return true;
        }

        private List<ReportItem> GetReportItems(string file, char separator, int userId, int reportGMT)
        {
            string dateFormat = "";
            Dictionary<string, int> headers;
            List<Dictionary<string, int>> headersList;
            var items = new List<ReportItem>();
            try
            {
                //foreach (string file in SourceFiles)
                {
                    using (var reader = new StreamReader(file))
                    {
                        int processedIndex;
                        int resultIndex;

                        List<string> fileHeaders = reader.ReadLine().Split(separator).ToList();

                        headers = GetHeadersIndexes(_reportTypeConfiguration.ReportFields, fileHeaders, out processedIndex, out resultIndex);

                        //headersList = GetHeadersList(_reportConfiguration.Fields.Except(headers.Keys).ToList(), fileHeaders);
                        List<string> fields = _reportTypeConfiguration.ReportFields.Where(x => !string.IsNullOrEmpty(x.NameInFile)).Select(x => x.HeaderName).Except(headers.Keys).ToList();
                        headersList = GetHeadersList(fields, fileHeaders);

                        while (!reader.EndOfStream)
                        {
                            string[] lineArray = reader.ReadLine().Split(separator);

                            if (processedIndex == -1 || resultIndex == -1 || lineArray.Length <= resultIndex)
                            {
                                continue;
                            }

                            if (lineArray[processedIndex] != Constants.PROCESS_RESULT_OK)
                            {
                                continue;
                            }

                            string resultId = lineArray[resultIndex];

                            foreach (var dHeader in headersList)
                            {
                                bool hasValue = false;
                                var item = new ReportItem();
                                foreach (int specialValue in dHeader.Values)
                                {
                                    string fileValue = lineArray[specialValue];
                                    if (!string.IsNullOrEmpty(fileValue))
                                    {
                                        item.AddValue(fileValue.Trim());
                                        hasValue = true;
                                    }
                                }

                                if (hasValue)
                                {
                                    foreach (int fixValue in headers.Values)
                                    {
                                        item.AddValue(lineArray[fixValue].Trim(), fixValue);
                                    }
                                    item.ResultId = resultId;
                                    items.Add(item);
                                }
                            }
                        }
                    }
                }

                GetDataFromDB(items, dateFormat, userId, reportGMT);

                return items;
            }
            catch (Exception e)
            {
                _logger.Error("Error trying to get report items");
                throw;
            }
        }

        private List<Dictionary<string, int>> GetHeadersList(List<string> fields, List<string> fileHeaders)
        {
            var headerDictionaryList = new List<Dictionary<string, int>>();

            int index = 1;
            int i = 0;
            int j = 0;
            while (i < fileHeaders.Count)
            {
                var headerDictionary = new Dictionary<string, int>();
                foreach (string field in fields)
                {
                    string headerNumber = $"{index}{field}";
                    j = i;
                    while (j < fileHeaders.Count)
                    {
                        string fileHeader = fileHeaders[j];
                        if (fileHeader.Contains(headerNumber))
                        {
                            headerDictionary.Add(field, j);
                            j++;
                            break;
                        }
                        j++;
                    }
                }

                if (headerDictionary.Count > 0)
                {
                    headerDictionaryList.Add(headerDictionary);
                }
                index++;
                i = j;
            }

            return headerDictionaryList;
        }

        protected Dictionary<string, int> GetHeadersIndexes(List<ReportFieldConfiguration> reportHeaders, List<string> fileHeaders, out int processedIndex, out int resultIndex)
        {
            var _headerList = new List<string>();
            var headers = new Dictionary<string, int>();

            foreach (ReportFieldConfiguration header in reportHeaders)
            {
                int index = fileHeaders.IndexOf(header.NameInFile);
                if (index != -1 && !headers.ContainsKey(header.NameInFile))
                {
                    headers.Add(header.NameInFile, index);
                }

                if (!_headerList.Contains(header.HeaderName) && !header.HeaderName.Equals("*"))
                {
                    _headerList.Add(header.HeaderName);
                }
            }

            if (reportHeaders.Exists(x => x.HeaderName.Equals("*")))
            {
                string header;
                for (int i = 0; i < fileHeaders.Count; i++)
                {
                    header = fileHeaders[i];
                    if (header != Constants.HEADER_PROCESS_RESULT
                        && header != Constants.HEADER_MESSAGE_ID
                        && header != Constants.HEADER_DELIVERY_RESULT
                        && header != Constants.HEADER_DELIVERY_LINK
                        && !headers.ContainsKey(header)
                        && !reportHeaders.Exists(x => x.NameInFile == header))
                    {
                        headers.Add(header, i);
                        if (!_headerList.Contains(header))
                        {
                            _headerList.Add(header);
                        }
                    }
                }
            }

            processedIndex = fileHeaders.IndexOf(Constants.HEADER_PROCESS_RESULT);
            resultIndex = fileHeaders.IndexOf(Constants.HEADER_MESSAGE_ID);

            return headers;
        }

        protected void GetDataFromDB(List<ReportItem> items, string dateFormat, int userId, int reportGMT)
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

                    List<DBStatusReportItem> dbReportItemList = sqlHelper.GetResultsByDeliveryList(userId, aux);
                    foreach (DBStatusReportItem dbReportItem in dbReportItemList)
                    {
                        ReportItem item = items.FirstOrDefault(x => x.ResultId == dbReportItem.MessageGuid);
                        if (item != null)
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
                                }
                            }
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

        protected override bool IsTimeToRun(IUserConfiguration user)
        {
            return true;
        }
    }
}
