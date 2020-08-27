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
            string reportFileName = $@"{_configuration.ReportsFolder}\reports.{DateTime.UtcNow.ToString("yyyyMMdd")}.json";

            if (File.Exists(reportFileName))
            {
                return;
            }

            string[] files = Directory.GetFiles($"{_configuration.ReportsFolder}", "*.json");

            List<ReportExecution> allReports = new List<ReportExecution>();

            foreach (string file in files)
            {
                string json = File.ReadAllText(file);

                List<ReportExecution> executions = JsonConvert.DeserializeObject<List<ReportExecution>>(json);

                allReports.AddRange(executions);
            }

            List<IUserConfiguration> reportUsers = _users.Where(x => x.Reports != null).ToList();
            List<ReportExecution> requests = new List<ReportExecution>();

            foreach (IUserConfiguration user in reportUsers)
            {
                foreach (ReportTypeConfiguration reportType in user.Reports.ReportsList)
                {
                    var lastExecution = allReports
                        .Where(x => x.UserName == user.Name && x.ReportId == reportType.ReportId)
                        .OrderByDescending(x => x.NextRun)
                        .FirstOrDefault();

                    List<ReportExecution> executionLists = reportType.GetReportExecution(user, lastExecution);
                    requests.AddRange(executionLists);
                }
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

        public void Process()
        {
            while (true)
            {
                try
                {
                    CheckConfigChanges();

                    CreateReportsFile();

                    string[] files = Directory.GetFiles($"{_configuration.ReportsFolder}", "*.json");

                    foreach (string file in files)
                    {
                        string json = File.ReadAllText(file);

                        List<ReportExecution> reports = JsonConvert.DeserializeObject<List<ReportExecution>>(json);

                        var processedReports = new List<ReportExecution>();

                        foreach (ReportExecution reportExecution in reports.Where(x => !x.Processed && x.RunDate < DateTime.UtcNow))
                        {
                            try
                            {
                                IUserConfiguration user = _users.Where(x => x.Name == reportExecution.UserName).FirstOrDefault();

                                ReportTypeConfiguration reportType = user.Reports.ReportsList.Where(x => x.ReportId == reportExecution.ReportId).FirstOrDefault();

                                ReportProcessor reportProcessor = reportType.GetReportProcessor(_configuration, _logger);

                                reportProcessor.Process(user, reportExecution);

                                processedReports.Add(reportExecution);
                            }
                            catch (Exception e)
                            {
                                _logger.Error($"Error to generate report:{reportExecution.ReportId} for user:{reportExecution.UserName} -- {e}");
                            }
                        }

                        if (processedReports.Any())
                        {
                            // vuelvo a levantar el archivo por si tuvo cambios de afuera para no perderlos.
                            // cuando sea por usuario no deberia tener mas problemas igual deberia ser algo mas seguro
                            json = File.ReadAllText(file);
                            reports = JsonConvert.DeserializeObject<List<ReportExecution>>(json);

                            foreach (ReportExecution processedReport in processedReports)
                            {
                                var report = reports.FirstOrDefault(x => x.UserName == processedReport.UserName && x.ReportId == processedReport.ReportId);

                                if (report != null)
                                {
                                    report.Processed = processedReport.Processed;
                                    report.ProcessedDate = processedReport.ProcessedDate;
                                    report.ReportFile = processedReport.ReportFile;
                                }
                            }

                            json = JsonConvert.SerializeObject(reports);
                            using (var streamWriter = new StreamWriter(file, false))
                            {
                                streamWriter.Write(json);
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
    }
}
