using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Relay.BulkSenderService.Classes
{
    public class ZipHelper
    {
        private readonly ILog _logger;

        public ZipHelper(ILog logger)
        {
            _logger = logger;
        }

        public List<string> UnzipFile(string zipFile, string unzipFolder, string extension = null)
        {
            string newFileName = null;

            var files = new List<string>();

            using (ZipArchive zipArchive = ZipFile.OpenRead(zipFile))
            {
                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                {
                    if (!string.IsNullOrEmpty(extension))
                    {
                        newFileName = $@"{unzipFolder}\{Path.GetFileNameWithoutExtension(entry.FullName)}.{extension}";
                    }
                    else
                    {
                        newFileName = $@"{unzipFolder}\{Path.GetFileName(entry.FullName)}";
                    }

                    entry.ExtractToFile(newFileName, true);

                    files.Add(newFileName);
                }
            }

            return files;
        }

        public void UnzipAll(string fileName, string folder)
        {
            using (Ionic.Zip.ZipFile zipFile = Ionic.Zip.ZipFile.Read(fileName))
            {
                foreach (var zipEntry in zipFile.Entries)
                {
                    try
                    {
                        zipEntry.Extract(folder);
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"ERROR Unzipping folder:{e}");
                    }
                }
            }
        }

        public void ZipFiles(List<string> files, string zipFile)
        {
            using (ZipArchive zipArchive = ZipFile.Open(zipFile, ZipArchiveMode.Create))
            {
                foreach (string file in files)
                {
                    if (File.Exists(file))
                    {
                        zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
                }
            }
        }
    }
}
