using System.Configuration;

namespace Relay.BulkSenderService.Configuration
{
    public class AppConfiguration : IConfiguration
    {
        public string SmtpHost
        {
            get { return ConfigurationManager.AppSettings["smtpHost"]; }
        }

        public int SmtpPort
        {
            get { return int.Parse(ConfigurationManager.AppSettings["smtpPort"]); }
        }

        public string LocalDownloadFolder
        {
            get { return ConfigurationManager.AppSettings["LocalDownloadFolder"]; }
        }

        public string BaseUrl
        {
            get { return ConfigurationManager.AppSettings["BaseUrl"]; }
        }

        public string TemplateUrl
        {
            get { return ConfigurationManager.AppSettings["TemplateUrl"]; }
        }

        public string AccountUrl
        {
            get { return ConfigurationManager.AppSettings["AccountUrl"]; }
        }

        public int FtpListInterval
        {
            get { return int.Parse(ConfigurationManager.AppSettings["FtpListInterval"]); }
        }

        public int MaxNumberOfThreads
        {
            get { return int.Parse(ConfigurationManager.AppSettings["MaxNumberOfThreads"]); }
        }

        public int BulkEmailCount
        {
            get { return int.Parse(ConfigurationManager.AppSettings["BulkEmailCount"]); }
        }

        public int ReportsInterval
        {
            get { return int.Parse(ConfigurationManager.AppSettings["ReportsInterval"]); }
        }

        public string AdminUser
        {
            get { return ConfigurationManager.AppSettings["AdminUser"]; }
        }

        public string AdminPass
        {
            get { return ConfigurationManager.AppSettings["AdminPass"]; }
        }

        public int CleanInterval
        {
            get { return int.Parse(ConfigurationManager.AppSettings["CleanInterval"]); }
        }

        public int CleanDays
        {
            get { return int.Parse(ConfigurationManager.AppSettings["CleanDays"]); }
        }

        public int LocalFilesInterval
        {
            get { return int.Parse(ConfigurationManager.AppSettings["LocalFilesInterval"]); }
        }

        public int CleanAttachmentsDays
        {
            get { return int.Parse(ConfigurationManager.AppSettings["CleanAttachmentsDays"]); }
        }

        public string ReportsFolder
        {
            get { return ConfigurationManager.AppSettings["ReportsFolder"]; }
        }

        public int PreProcessorInterval
        {
            get { return int.Parse(ConfigurationManager.AppSettings["PreProcessorInterval"]); }
        }

        public string UserFiles
        {
            get { return ConfigurationManager.AppSettings["UserFiles"]; }
        }

        public string PublicUserFiles
        {
            get { return ConfigurationManager.AppSettings["PublicUserFiles"]; }
        }

        public int DeliveryRetryCount
        {
            get { return int.Parse(ConfigurationManager.AppSettings["DeliveryRetryCount"]); }
        }

        public int DeliveryRetryInterval
        {
            get { return int.Parse(ConfigurationManager.AppSettings["DeliveryRetryInterval"]); }
        }

        public int DeliveryFailCount
        {
            get { return int.Parse(ConfigurationManager.AppSettings["DeliveryFailCount"]); }
        }

        public int StatusProcessorInterval
        {
            get { return int.Parse(ConfigurationManager.AppSettings["StatusProcessorInterval"]); }
        }
    }
}
