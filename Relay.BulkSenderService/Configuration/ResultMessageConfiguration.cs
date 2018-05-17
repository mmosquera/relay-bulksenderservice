using System.IO;

namespace Relay.BulkSenderService.Configuration
{
    public class ResultMessageConfiguration : IResultConfiguration
    {
        public string Folder { get; set; }
        public string Message { get; set; }
        public IReportName FileName { get; set; }

        public IResultConfiguration Clone()
        {
            var resultMessageConfiguration = new ResultMessageConfiguration();

            resultMessageConfiguration.Folder = this.Folder;
            resultMessageConfiguration.Message = this.Message;

            if (this.FileName != null)
            {
                resultMessageConfiguration.FileName = this.FileName.Clone();
            }

            return resultMessageConfiguration;
        }

        public string SaveAndGetName(string fileName, string resultsFolder)
        {
            string resultsFileName = FileName.GetReportName(fileName);
            string resultsFileNamePath = $@"{resultsFolder}\{resultsFileName}";

            // TODO: Remove save file from here! Find better approach.
            try
            {
                using (var streamWriter = new StreamWriter(resultsFileNamePath))
                {
                    streamWriter.Write($"{Message}");
                }
            }
            catch { }

            return resultsFileNamePath;
        }
    }
}