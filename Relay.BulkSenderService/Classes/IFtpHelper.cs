using System.Collections.Generic;

namespace Relay.BulkSenderService.Classes
{
    public interface IFtpHelper
    {
        List<string> GetFileList(string folder, IEnumerable<string> filters);

        bool DownloadFile(string ftpFileName, string localFileName);

        bool DownloadFileWithResume(string ftpFileName, string localFileName);

        bool UploadFile(string localFileName, string ftpFileName);

        bool DeleteFile(string ftpFileName);

        void UploadFileAsync(string localFileName, string ftpFileName);
    }
}
