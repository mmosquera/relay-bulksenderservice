using Newtonsoft.Json;
using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.IO;
using System.Text;

namespace Relay.BulkSenderService.Processors.Status
{
    public class FtpStatusProcessor : StatusProcessor
    {
        public FtpStatusProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
        }

        public override void ProcessStatusFile(IUserConfiguration userConfiguration, string fileName)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            try
            {

                _logger.Debug($"Start to process status file for user {userConfiguration.Name}");

                string jsonContent;

                using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var streamReader = new StreamReader(fileStream))
                {
                    jsonContent = streamReader.ReadToEnd();
                }

                UserFilesStatus userFilesStatus = JsonConvert.DeserializeObject<UserFilesStatus>(jsonContent);

                var stringBuilder = new StringBuilder();

                foreach (FileStatus fileStatus in userFilesStatus.Files)
                {
                    string line = $"NOMBRE={fileStatus.FileName}|TOTAL={fileStatus.Total}|PROCESADAS={fileStatus.Processed}|FECHA={fileStatus.LastUpdate.AddHours(-3)}";
                    stringBuilder.AppendLine(line);
                }

                var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

                string resultsFilePath = $@"{filePathHelper.GetReportsFilesFolder()}\status.{DateTime.UtcNow.AddHours(-3).ToString("yyyyMMddhhmm")}.txt";

                using (var streamWriter = new StreamWriter(resultsFilePath))
                {
                    streamWriter.Write(stringBuilder.ToString());
                }

                string ftpFileName = $@"{userConfiguration.Results.Folder}/{Path.GetFileName(resultsFilePath)}";

                var ftpHelper = userConfiguration.Ftp.GetFtpHelper(_logger);

                ftpHelper.UploadFile(resultsFilePath, ftpFileName);
            }
            catch (Exception e)
            {
                _logger.Error($"Ftp Status error:{e}");
            }
        }
    }
}
