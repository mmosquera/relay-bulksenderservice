using Newtonsoft.Json;
using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Reports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Relay.BulkSenderService.Processors
{
    public class ReportGenerator : BaseWorker
    {
        public ReportGenerator(ILog logger, IConfiguration configuration)
            : base(logger, configuration)
        {
        }

        private void CreateReportsFile()
        {
            List<IUserConfiguration> reportUsers = _users.Where(x => x.Reports != null).ToList();

            foreach (IUserConfiguration user in reportUsers)
            {
                var filePathHelper = new FilePathHelper(_configuration, user.Name);

                string reportFileName = $@"{filePathHelper.GetReportsFilesFolder()}\reports.{user.Name}.{DateTime.UtcNow.ToString("yyyyMMdd")}.json";

                if (File.Exists(reportFileName))
                {
                    continue;
                }

                var requests = new List<ReportExecution>();

                string[] files = Directory.GetFiles(filePathHelper.GetReportsFilesFolder(), "*.json");

                List<ReportExecution> allReports = new List<ReportExecution>();

                foreach (string file in files)
                {
                    string json = File.ReadAllText(file);

                    List<ReportExecution> executions = JsonConvert.DeserializeObject<List<ReportExecution>>(json);

                    allReports.AddRange(executions);
                }

                foreach (ReportTypeConfiguration reportType in user.Reports.ReportsList)
                {
                    var lastExecution = allReports
                        .Where(x => x.UserName == user.Name && x.ReportId == reportType.ReportId)
                        .OrderByDescending(x => x.NextRun)
                        .FirstOrDefault();

                    List<ReportExecution> executionLists = reportType.GetReportExecution(user, lastExecution);
                    requests.AddRange(executionLists);
                }

                if (requests.Count > 0)
                {
                    string reports = JsonConvert.SerializeObject(requests);
                    using (var streamWriter = new StreamWriter(reportFileName, false))
                    {
                        streamWriter.Write(reports);
                    }
                }
            }
        }

        public void Process()
        {
            while (true)
            {
                try
                {
                    CheckConfigChanges();

                    //TODO: en el proximo cambio sacar este metodo.
                    MigrateReportsJsonFiles();

                    CreateReportsFile();

                    List<IUserConfiguration> reportUsers = _users.Where(x => x.Reports != null).ToList();

                    foreach (IUserConfiguration userConfiguration in reportUsers)
                    {
                        var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

                        string[] files = Directory.GetFiles(filePathHelper.GetReportsFilesFolder(), "*.json");

                        foreach (string file in files)
                        {
                            string json = File.ReadAllText(file);

                            List<ReportExecution> reports = JsonConvert.DeserializeObject<List<ReportExecution>>(json);

                            bool hasChanges = false;

                            foreach (ReportExecution reportExecution in reports.Where(x => !x.Processed && x.RunDate < DateTime.UtcNow))
                            {
                                try
                                {
                                    ReportProcessor reportProcessor = userConfiguration.GetReportProcessor(_logger, _configuration, reportExecution.ReportId);

                                    //TODO: por ahi podemos procesar uno por usuario.
                                    reportProcessor.Process(userConfiguration, reportExecution);

                                    hasChanges = true;
                                }
                                catch (Exception e)
                                {
                                    _logger.Error($"Error to generate report:{reportExecution.ReportId} for user:{userConfiguration.Name} -- {e}");
                                }
                            }

                            if (hasChanges)
                            {
                                json = JsonConvert.SerializeObject(reports);
                                using (var streamWriter = new StreamWriter(file, false))
                                {
                                    streamWriter.Write(json);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error($"General error on Generate report -- {e}");
                }

                Thread.Sleep(_configuration.ReportsInterval);
            }
        }

        private void MigrateReportsJsonFiles()
        {
            IEnumerable<IUserConfiguration> reportUsers = _users.Where(x => x.Reports != null);

            foreach(IUserConfiguration reportUser in reportUsers)
            {

            }
        }
    }
}
