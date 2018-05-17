namespace Relay.BulkSenderService.Configuration
{
    public class ResultConfiguration : IResultConfiguration
    {
        public string Folder { get; set; }
        public IReportName FileName { get; set; }

        public IResultConfiguration Clone()
        {
            var resultConfiguration = new ResultConfiguration();

            resultConfiguration.Folder = this.Folder;

            if (this.FileName != null)
            {
                resultConfiguration.FileName = this.FileName.Clone();
            }

            return resultConfiguration;
        }

        public string SaveAndGetName(string fileName, string resultsFolder)
        {
            string resultsFileName = FileName.GetReportName(fileName);
            string resultsFileNamePath = $@"{resultsFolder}\{resultsFileName}";

            return resultsFileNamePath;
        }
    }
}
