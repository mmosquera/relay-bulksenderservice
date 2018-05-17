namespace Relay.BulkSenderService.Configuration
{
    public interface IResultConfiguration
    {
        string Folder { get; set; }
        IReportName FileName { get; set; }

        IResultConfiguration Clone();
        string SaveAndGetName(string fileName, string resultsFolder);
    }
}
