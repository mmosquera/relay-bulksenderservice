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

                if (userFilesStatus.Files.Count == 0)
                {
                    return;
                }

                var stringBuilder = new StringBuilder();

                stringBuilder.AppendLine("NOMBRE|TOTAL|PROCESADOS|FECHA");

                foreach (FileStatus fileStatus in userFilesStatus.Files)
                {
                    string datatime = fileStatus.LastUpdate
                        .AddHours(userConfiguration.UserGMT)
                        .ToString(((FtpStatusConfiguration)userConfiguration.Status).StatusFileDateFormat);

                    string line = $"{fileStatus.FileName}|{fileStatus.Total}|{fileStatus.Processed}|{datatime}";

                    stringBuilder.AppendLine(line);
                }

                var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

                string resultsFilePath = $@"{filePathHelper.GetReportsFilesFolder()}\status.{DateTime.UtcNow.AddHours(userConfiguration.UserGMT).ToString("yyyyMMddhhmm")}.txt";

                using (var streamWriter = new StreamWriter(resultsFilePath))
                {
                    streamWriter.Write(stringBuilder.ToString());
                }

                string ftpFileName = $@"{((FtpStatusConfiguration)userConfiguration.Status).FtpFolder}/{Path.GetFileName(resultsFilePath)}";

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
