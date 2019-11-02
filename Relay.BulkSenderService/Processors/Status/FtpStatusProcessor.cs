﻿using Newtonsoft.Json;
using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Relay.BulkSenderService.Processors.Status
{
    public class FtpStatusProcessor : StatusProcessor
    {
        public FtpStatusProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
        }

        public override void ProcessStatusFile(IUserConfiguration userConfiguration, List<string> statusFiles)
        {
            if (statusFiles.Count == 0)
            {
                return;
            }

            try
            {
                _logger.Debug($"Start to process status file for user {userConfiguration.Name}");

                var filesToDelete = new List<string>();

                string jsonContent;
                var stringBuilder = new StringBuilder();

                foreach (string fileName in statusFiles)
                {
                    int retries = 0;
                    while (IsFileInUse(fileName) && retries < 3)
                    {
                        Thread.Sleep(TIME_WAIT_FILE);
                        retries++;
                    }

                    using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var streamReader = new StreamReader(fileStream))
                    {
                        jsonContent = streamReader.ReadToEnd();
                    }

                    FileStatus fileStatus = JsonConvert.DeserializeObject<FileStatus>(jsonContent);

                    string datatime = fileStatus.LastUpdate
                        .AddHours(userConfiguration.UserGMT)
                        .ToString(((FtpStatusConfiguration)userConfiguration.Status).StatusFileDateFormat);

                    string line = $"{fileStatus.FileName}|{fileStatus.Total}|{fileStatus.Processed}|{datatime}";

                    stringBuilder.AppendLine(line);

                    if (fileStatus.Finished)
                    {
                        filesToDelete.Add(fileName);
                    }
                }

                var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

                string resultsFilePath = $@"{filePathHelper.GetReportsFilesFolder()}\status.{DateTime.UtcNow.AddHours(userConfiguration.UserGMT).ToString("yyyyMMddhhmm")}.txt";

                using (var streamWriter = new StreamWriter(resultsFilePath))
                {
                    streamWriter.Write(stringBuilder);
                }

                string ftpFileName = $@"{((FtpStatusConfiguration)userConfiguration.Status).FtpFolder}/{Path.GetFileName(resultsFilePath)}";

                var ftpHelper = userConfiguration.Ftp.GetFtpHelper(_logger);

                ftpHelper.UploadFile(resultsFilePath, ftpFileName);

                foreach (string fileToDelete in filesToDelete)
                {
                    File.Delete(fileToDelete);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"FTP STATUS PROCESSOR ERROR:{e}");
            }
        }
    }
}
