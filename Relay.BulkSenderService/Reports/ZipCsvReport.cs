using Relay.BulkSenderService.Classes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Relay.BulkSenderService.Reports
{
    public class ZipCsvReport : CsvReport
    {
        public ZipCsvReport()
        {
        }

        protected override void Save()
        {
            _reportFileName = $@"{ReportPath}\{ReportName}";

            if (Path.GetExtension(_reportFileName).Equals(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                // TODO: Improve entry file name. Using type for extension.
                string tempCsv = _reportFileName.Replace(".ZIP", ".TXT");

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
