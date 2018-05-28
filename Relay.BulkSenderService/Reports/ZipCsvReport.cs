using Relay.BulkSenderService.Classes;
using System.Collections.Generic;
using System.IO;

namespace Relay.BulkSenderService.Reports
{
    public class ZipCsvReport : CsvReport
    {
        public ZipCsvReport(ILog logger) : base(logger)
        {
        }

        protected override void Save()
        {
            _reportFileName = $@"{ReportPath}\{ReportName}";

            if (Path.GetExtension(_reportFileName).Equals(".zip"))
            {
                string tempCsv = _reportFileName.Replace(".zip", ".csv");

                using (var streamWriter = new StreamWriter(tempCsv))
                {
                    streamWriter.Write(_stringBuilder.ToString());
                }

                var zipHelper = new ZipHelper();
                zipHelper.ZipFiles(new List<string>() { tempCsv }, _reportFileName);

                File.Delete(tempCsv);
            }
        }
    }
}
