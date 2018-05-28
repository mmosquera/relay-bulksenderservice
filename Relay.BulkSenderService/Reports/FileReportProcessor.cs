﻿using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Relay.BulkSenderService.Reports
{
    public class FileReportProcessor : ReportProcessor
    {
        public FileReportProcessor(IConfiguration configuration, ILog logger, ReportTypeConfiguration reportTypeConfiguration)
            : base(logger, configuration, reportTypeConfiguration)
        {

        }

        protected override List<string> GetFilesToProcess(IUserConfiguration user)
        {
            var fileList = new List<string>();
            var filePathHelper = new FilePathHelper(_configuration, user.Name);
            var directoryInfo = new DirectoryInfo(filePathHelper.GetResultsFilesFolder());

            FileInfo[] files = directoryInfo.GetFiles("*.sent");

            foreach (var file in files)
            {
                ITemplateConfiguration templateConfiguration = ((UserApiConfiguration)user).GetTemplateConfiguration(file.FullName);

                if (_reportTypeConfiguration.Templates.Contains(templateConfiguration.TemplateName)
                    && DateTime.UtcNow.Subtract(file.CreationTimeUtc).TotalHours > _reportTypeConfiguration.Hour)
                {
                    fileList.Add(file.FullName);
                }
            }

            return fileList;
        }

        protected override void ProcessFilesForReports(List<string> files, IUserConfiguration user)
        {
            if (files.Count == 0)
            {
                return;
            }

            var ftpHelper = user.Ftp.GetFtpHelper(_logger);
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            foreach (string file in files)
            {
                _logger.Debug($"Create report file with {file} for user {user.Name}.");

                var report = new ExcelReport(_logger, _reportTypeConfiguration);
                //report.SourceFile = file;
                //report.Separator = _reportTypeConfiguration.FieldSeparator;
                report.CustomItems = GetCustomItems(file, user.UserGMT);
                report.ReportPath = filePathHelper.GetReportsFilesFolder();
                report.ReportGMT = user.UserGMT;
                report.UserId = user.Credentials.AccountId;

                string reportFileName = report.Generate();

                if (!string.IsNullOrEmpty(reportFileName) && File.Exists(reportFileName))
                {
                    string renameFile = file.Replace(".sent", ".report");
                    File.Move(file, renameFile);
                    UploadFileToFtp(reportFileName, ((UserApiConfiguration)user).Reports.Folder, ftpHelper);
                }
            }
        }

        public override bool GenerateForcedReport(List<string> files, IUserConfiguration user)
        {
            List<string> filteredFiles = FilterFilesByTemplate(files, user);

            if (filteredFiles.Count == 0)
            {
                return false;
            }

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            foreach (string file in filteredFiles)
            {
                _logger.Debug($"Create report file with {file} for user {user.Name}.");

                var report = new ExcelReport(_logger, _reportTypeConfiguration)
                {
                    //SourceFile = file,
                    //Separator = _reportTypeConfiguration.FieldSeparator,
                    CustomItems = GetCustomItems(file, user.UserGMT),
                    ReportPath = filePathHelper.GetForcedReportsFolder(),
                    ReportGMT = user.UserGMT,
                    UserId = user.Credentials.AccountId
                };

                report.Generate();
            }

            return true;
        }

        private Dictionary<int, List<string>> GetCustomItems(string sourceFile, int reportGMT)
        {
            var customItems = new Dictionary<int, List<string>>();

            if (_reportTypeConfiguration.ReportItems != null)
            {
                foreach (ReportItemConfiguration riConfiguration in _reportTypeConfiguration.ReportItems)
                {
                    customItems.Add(riConfiguration.Row, riConfiguration.Values);
                }
            }

            string sendDate = new FileInfo(sourceFile).CreationTimeUtc.AddHours(reportGMT).ToString("yyyyMMdd");
            var customValues = new List<List<string>>()
            {
                new List<string>() { "Información de envio" },
                new List<string>() { "Nombre", Path.GetFileNameWithoutExtension(sourceFile) },
                new List<string>() { "Fecha de envio", sendDate },
                new List<string>() { "Detalle suscriptores" }
            };
            int index = 0;
            foreach (List<string> value in customValues)
            {
                while (customItems.ContainsKey(index))
                {
                    index++;
                }
                customItems.Add(index, value);
                index++;
            }

            return customItems;
        }

        protected override bool IsTimeToRun(IUserConfiguration user)
        {
            return true;
        }
    }
}
