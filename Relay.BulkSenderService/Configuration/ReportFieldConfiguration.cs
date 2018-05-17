namespace Relay.BulkSenderService.Configuration
{
    public class ReportFieldConfiguration
    {
        public string HeaderName { get; set; }
        public int Position { get; set; }
        public string NameInFile { get; set; }
        public string NameInDB { get; set; }

        public ReportFieldConfiguration Clone()
        {
            var reportFieldConfiguration = new ReportFieldConfiguration();

            reportFieldConfiguration.HeaderName = this.HeaderName;
            reportFieldConfiguration.Position = this.Position;
            reportFieldConfiguration.NameInFile = this.NameInFile;
            reportFieldConfiguration.NameInDB = this.NameInDB;

            return reportFieldConfiguration;
        }
    }
}
