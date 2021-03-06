﻿using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Classes
{
    public class SftpHelper : AbstractFtpHelper
    {
        public SftpHelper(ILog logger, string host, int port, string user, string password) : base(logger, host, port, user, password)
        {
        }

        public override bool DeleteFile(string ftpFileName)
        {
            try
            {
                using (SftpClient sftp = new SftpClient(_ftpHost, _port, _ftpUser, _ftpPassword))
                {
                    sftp.Connect();

                    sftp.DeleteFile(ftpFileName);

                    sftp.Disconnect();
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.Error($"Error on delete file -- {e}");

                return false;
            }
        }

        public override bool DownloadFile(string ftpFileName, string localFileName)
        {
            try
            {
                using (SftpClient sftp = new SftpClient(_ftpHost, _port, _ftpUser, _ftpPassword))
                {
                    sftp.Connect();

                    using (Stream fileStream = File.OpenWrite(localFileName))
                    {
                        sftp.DownloadFile(ftpFileName, fileStream);
                    }

                    sftp.Disconnect();
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.Error($"Error download file -- {e}");

                return false;
            }
        }

        public override List<string> GetFileList(string folder, IEnumerable<string> filters)
        {
            var files = new List<string>();

            try
            {
                using (SftpClient sftp = new SftpClient(_ftpHost, _port, _ftpUser, _ftpPassword))
                {
                    sftp.Connect();

                    var sftpFiles = sftp.ListDirectory(folder);

                    foreach (var file in sftpFiles)
                    {
                        if (file.IsRegularFile && filters.Any(f => file.Name.EndsWith(f)))
                        {
                            files.Add(file.Name);
                        }
                    }

                    sftp.Disconnect();
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error retriving list -- {e}");
            }

            return files;
        }

        public override bool UploadFile(string localFileName, string ftpFileName)
        {
            if (!File.Exists(localFileName))
            {
                return false;
            }

            try
            {
                using (var sftp = new SftpClient(_ftpHost, _port, _ftpUser, _ftpPassword))
                {
                    sftp.Connect();

                    using (var fileStream = new FileStream(localFileName, FileMode.Open))
                    {
                        sftp.UploadFile(fileStream, ftpFileName);
                    }

                    sftp.Disconnect();
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.Error($"Error upload to sftp -- {e}");

                return false;
            }
        }
    }
}
