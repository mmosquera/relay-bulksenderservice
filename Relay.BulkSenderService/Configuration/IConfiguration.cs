namespace Relay.BulkSenderService.Configuration
{
    public interface IConfiguration
    {
        string LocalDownloadFolder { get; }
        string SmtpHost { get; }
        int SmtpPort { get; }
        int DeliveryInterval { get; }
        string BaseUrl { get; }
        string TemplateUrl { get; }
        string AccountUrl { get; }
        int FtpListInterval { get; }
        int MaxNumberOfThreads { get; }
        int BulkEmailCount { get; }
        int ReportsInterval { get; }
        string AdminUser { get; }
        string AdminPass { get; }
        int CleanInterval { get; }
        int CleanDays { get; }
        int LocalFilesInterval { get; }
    }
}
