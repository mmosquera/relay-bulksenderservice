﻿namespace Relay.BulkSenderService.Configuration
{
    public class ResultConfiguration : IResultConfiguration
    {
        public string Folder { get; set; }
        public IReportName FileName { get; set; }

        public string SaveAndGetName(string fileName, string resultsFolder)
        {
            string resultsFileName = FileName.GetReportName(fileName);
            string resultsFileNamePath = $@"{resultsFolder}\{resultsFileName}";

            return resultsFileNamePath;
        }
    }
}
