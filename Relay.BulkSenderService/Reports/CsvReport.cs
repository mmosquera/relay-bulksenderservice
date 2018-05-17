using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Relay.BulkSenderService.Reports
{
    /// <summary>
    /// anda con * para todos los campos , no anda para algunos y * 
    /// </summary>
    public class CsvReport : ReportBase
    {
        private StringBuilder _stringBuilder;

        public CsvReport(ILog logger, ReportTypeConfiguration reportConfiguration)
            : base(logger, reportConfiguration)
        {
            _stringBuilder = new StringBuilder();
            _dateFormat = "dd/MM/yyyy HH:mm";
        }

        protected override void FillItems()
        {
            try
            {
                foreach (string file in SourceFiles)
                {
                    using (var streamReader = new StreamReader(file))
                    {
                        List<string> fileHeaders = streamReader.ReadLine().Split(Separator).ToList();

                        // Contiene el header(key) y la posicion(value) en el archivo original, que seran incluidos en el reporte.
                        Dictionary<string, int> headers = GetHeadersIndexes(_reportConfiguration.ReportFields, fileHeaders, out int processedIndex, out int resultIndex);

                        if (processedIndex == -1 || resultIndex == -1)
                        {
                            continue;
                        }

                        while (!streamReader.EndOfStream)
                        {
                            string[] lineArray = streamReader.ReadLine().Split(Separator);

                            if (lineArray.Length <= resultIndex || lineArray[processedIndex] != Constants.PROCESS_RESULT_OK)
                            {
                                continue;
                            }

                            var item = new ReportItem();

                            foreach (string key in headers.Keys)
                            {
                                //index in original file.
                                int value = headers[key];

                                //index in report
                                int index = value;
                                ReportFieldConfiguration field = _reportConfiguration.ReportFields.FirstOrDefault(x => x.NameInFile == key);
                                if (field != null)
                                {
                                    index = field.Position;
                                }

                                item.AddValue(lineArray[value].Trim(), index);
                            }

                            item.ResultId = lineArray[resultIndex];

                            _items.Add(item);
                        }
                    }
                }

                GetDataFromDB(_items, _dateFormat);
            }
            catch (Exception)
            {
                _logger.Error("Error trying to get report items");
                throw;
            }
        }

        protected override void FillReport()
        {
            string headerLine = string.Join(_reportConfiguration.FieldSeparator.ToString(), _headerList);

            _stringBuilder.AppendLine(headerLine);

            foreach (ReportItem item in _items)
            {
                if (item.GetValues().Count == _headerList.Count)
                {
                    string itemLine = string.Join(_reportConfiguration.FieldSeparator.ToString(), item.GetValues());
                    _stringBuilder.AppendLine(itemLine);
                }
            }
        }

        protected override void Save()
        {
            _reportFileName = $@"{ReportPath}\{_reportConfiguration.Name.GetReportName("", ReportPath)}";

            using (var streamWriter = new StreamWriter(_reportFileName))
            {
                streamWriter.Write(_stringBuilder.ToString());
            }
        }
    }
}
