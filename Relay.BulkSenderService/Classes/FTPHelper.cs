﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace Relay.BulkSenderService.Classes
{
    public class FTPHelper : AbstractFtpHelper
    {
        private bool _isFTPS;

        public FTPHelper(ILog logger, string host, int port, string user, string password, bool ftps = false) : base(logger, host, port, user, password)
        {
            _isFTPS = ftps;
        }

        public override List<string> GetFileList(string folder, IEnumerable<string> filters)
        {
            var files = new List<string>();
            Uri requestUri = GetFtpUri(folder);
            if (requestUri == null)
            {
                return files;
            }

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(requestUri);
                request.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.UseBinary = true;
                request.EnableSsl = _isFTPS;
                request.UsePassive = true;
                request.KeepAlive = true;

                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            string extension = Path.GetExtension(line);
                            
                            if (filters.Any(f => extension.Equals(f, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                files.Add(Path.GetFileName(line));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error retriving list -- {e}");
            }

            return files;
        }

        /// <summary>
        /// Download file from FTP.
        /// </summary>
        /// <param name="fileName">The file to download.</param>
        public override bool DownloadFile(string ftpFileName, string localFileName)
        {
            Uri requestUri = GetFtpUri(ftpFileName);

            if (requestUri == null)
            {
                return false;
            }

            var downloadRequest = (FtpWebRequest)WebRequest.Create(requestUri);
            downloadRequest.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
            downloadRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            downloadRequest.UseBinary = true;
            downloadRequest.KeepAlive = true;
            downloadRequest.EnableSsl = _isFTPS;
            downloadRequest.UsePassive = true;

            try
            {
                using (var downloadResponse = (FtpWebResponse)downloadRequest.GetResponse())
                {
                    using (var responseStream = downloadResponse.GetResponseStream())
                    {
                        using (var writeStream = new FileStream(localFileName, FileMode.Create))
                        {
                            int length = 4096;
                            byte[] buffer = new byte[length];
                            int bytesRead;
                            while ((bytesRead = responseStream.Read(buffer, 0, length)) > 0)
                            {
                                writeStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.Error($"Error download file -- {e}");

                return false;
            }
        }

        /// <summary>
        /// Deletes file from FTP.
        /// </summary>
        /// <param name="fileName">The filename to delete.</param>
        /// <returns>True if the file was deleted.</returns>
        public override bool DeleteFile(string ftpFileName)
        {
            Uri requestUri = GetFtpUri(ftpFileName);

            if (requestUri == null)
            {
                return false;
            }

            var deleteRequest = (FtpWebRequest)WebRequest.Create(requestUri);
            deleteRequest.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
            deleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            deleteRequest.UseBinary = true;
            deleteRequest.EnableSsl = _isFTPS;
            deleteRequest.UsePassive = true;

            try
            {
                using (var deleteResponse = (FtpWebResponse)deleteRequest.GetResponse())
                {
                    var result = deleteResponse.StatusCode == FtpStatusCode.CommandOK
                        || deleteResponse.StatusCode == FtpStatusCode.FileActionOK;
                    return result;
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error on delete file -- {e}");

                return false;
            }
        }

        public bool RenameFile(string fileName, string newFileName)
        {
            Uri requestUri = GetFtpUri(fileName);
            if (requestUri == null)
            {
                return false;
            }

            var renameRequest = (FtpWebRequest)WebRequest.Create(requestUri);
            renameRequest.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
            renameRequest.Method = WebRequestMethods.Ftp.Rename;
            renameRequest.UseBinary = true;
            renameRequest.KeepAlive = true;
            renameRequest.EnableSsl = _isFTPS;
            renameRequest.UsePassive = true;
            renameRequest.RenameTo = newFileName;

            try
            {
                using (var renameResponse = (FtpWebResponse)renameRequest.GetResponse())
                {
                    var result = (renameResponse.StatusCode == FtpStatusCode.CommandOK
                        || renameResponse.StatusCode == FtpStatusCode.FileActionOK);
                    return result;
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error on rename file -- {e}");
                return false;
            }
        }

        public bool UploadFileWithResume(string localFileName, string ftpFileName)
        {
            long offset = 0;

            if (!File.Exists(localFileName))
            {
                return false;
            }

            Uri requestUri = GetFtpUri(ftpFileName);
            if (requestUri == null)
            {
                return false;
            }

            var sizeRequest = (FtpWebRequest)WebRequest.Create(requestUri);
            sizeRequest.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
            sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;
            sizeRequest.UseBinary = true;

            using (var sizeResponse = (FtpWebResponse)sizeRequest.GetResponse())
            {
                offset = sizeResponse.ContentLength;
            }

            FileStream fileStream = File.OpenRead(localFileName);

            if (fileStream.Length == offset) { return true; }

            var request = (FtpWebRequest)WebRequest.Create(requestUri);
            request.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
            request.Method = offset == 0 ? WebRequestMethods.Ftp.UploadFile : WebRequestMethods.Ftp.AppendFile;
            request.UseBinary = true;
            request.EnableSsl = _isFTPS;
            request.UsePassive = true;
            request.KeepAlive = false;

            int length = 4096;
            byte[] buffer = new byte[length];

            Stream requestStream = request.GetRequestStream();

            if (offset != 0)
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
            }

            int bytesRead = fileStream.Read(buffer, 0, length);
            while (bytesRead != 0)
            {
                requestStream.Write(buffer, 0, bytesRead);
                bytesRead = fileStream.Read(buffer, 0, length);
            }

            return true;
        }

        public override bool UploadFile(string localFileName, string ftpFileName)
        {
            if (!File.Exists(localFileName))
            {
                return false;
            }

            Uri requestUri = GetFtpUri(ftpFileName);
            if (requestUri == null)
            {
                return false;
            }

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(requestUri);
                request.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.UseBinary = true;
                request.EnableSsl = _isFTPS;
                request.UsePassive = true;
                request.KeepAlive = false;

                byte[] bytesArray = File.ReadAllBytes(localFileName);
                request.ContentLength = bytesArray.Length;

                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(bytesArray, 0, bytesArray.Length);
                }

                bool result;
                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    result = (response.StatusCode == FtpStatusCode.ClosingData
                        || response.StatusCode == FtpStatusCode.FileActionOK
                        || response.StatusCode == FtpStatusCode.CommandOK);
                }
                return result;
            }
            catch (Exception e)
            {
                _logger.Error($"Error on upload file -- {e}");
                return false;
            }
        }

        private Uri GetFtpUri(string fileName = "")
        {
            string uri = "";

            if (_port == 0)
            {
                uri = $"{_ftpHost}/{fileName}";
            }
            else
            {
                uri = $"{_ftpHost}:{_port}/{fileName}";
            }

            var requestUri = new Uri(uri);

            if (requestUri.Scheme == Uri.UriSchemeFtp)
            {
                return requestUri;
            }

            _logger.Error($"Invalid request uri: {uri}");

            return null;
        }
    }
}
