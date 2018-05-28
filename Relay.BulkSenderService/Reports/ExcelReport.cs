using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Reports
{
    public class ExcelReport : ReportBase
    {
        private ExcelHelper _excelHelper;
        //public string SourceFile;
        public Dictionary<int, List<string>> CustomItems { get; set; }

        public ExcelReport(ILog logger, ReportTypeConfiguration reportConfiguration)
            : base(logger)
        {
            _dateFormat = "yyyy-MM-dd HH:mm:ss";
        }

        protected override void FillReport()
        {
            //string reportName = _reportConfiguration.Name.GetReportName(Path.GetFileName(SourceFile), ReportPath);
            _reportFileName = $@"{ReportPath}\{ReportName}";
            _excelHelper = new ExcelHelper(_reportFileName, "Delivery Report");

            //Dictionary<int, List<string>> customItems = GetCustomItems();

            foreach (int key in CustomItems.Keys.OrderBy(t => t))
            {
                _excelHelper.GenerateReportRow(CustomItems[key]);
            }

            _excelHelper.GenerateReportRow(_headerList);

            foreach (ReportItem item in _items)
            {
                _excelHelper.GenerateReportRow(item.GetValues());
            }
        }

        protected override void Save()
        {
            _excelHelper.Save();
        }

        //protected void FillItems()
        //{
        //    Dictionary<string, int> headers;

        //    try
        //    {
        //        using (StreamReader streamReader = new StreamReader(SourceFile))
        //        {
        //            int processedIndex;
        //            int resultIndex;

        //            headers = GetHeadersIndexes(_reportConfiguration.ReportFields, streamReader.ReadLine().Split(Separator).ToList(), out processedIndex, out resultIndex);

        //            while (!streamReader.EndOfStream)
        //            {
        //                string[] lineArray = streamReader.ReadLine().Split(Separator);

        //                if (processedIndex == -1 || resultIndex == -1 || lineArray.Length <= resultIndex)
        //                {
        //                    continue;
        //                }

        //                if (lineArray[processedIndex] != Constants.PROCESS_RESULT_OK)
        //                {
        //                    continue;
        //                }

        //                var item = new ReportItem();

        //                foreach (int value in headers.Values)
        //                {
        //                    item.AddValue(lineArray[value].Trim());
        //                }

        //                item.ResultId = lineArray[resultIndex];

        //                _items.Add(item);
        //            }
        //        }

        //        GetDataFromDB(_items, _dateFormat);
        //    }
        //    catch (Exception)
        //    {
        //        _logger.Error("Error trying to get report items");
        //        throw;
        //    }
        //}
                
    }
}
