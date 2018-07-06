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
        private List<ReportExecution> _reports;

        public ReportGenerator(ILog logger, IConfiguration configuration, IWatcher watcher) : base(logger, configuration, watcher)
        {
            _reports = new List<ReportExecution>();
            ((FileCommandsWatcher)_watcher).GenerateReportEvent += ReportGenerator_GenerateReportEvent;
            LoadUserReports();
        }

        // TODO: check if needed use lock.
        private void ReportGenerator_GenerateReportEvent(object sender, ReportCommandsEventArgs e)
        {
            IUserConfiguration user = _users.FirstOrDefault(x => x.Name.Equals(e.User, StringComparison.InvariantCultureIgnoreCase));
            ReportTypeConfiguration reportType = user?.Reports?.ReportsList.FirstOrDefault(x => x.ReportId.Equals(e.Report, StringComparison.InvariantCultureIgnoreCase));

            if (reportType != null)
            {
                var reportExecution = new ReportExecution()
                {
                    UserName = user.Name,
                    ReportId = reportType.ReportId,
                    NextRun = e.End,
                    LastRun = e.Start
                };

                Thread threadReport = new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        _logger.Debug(string.Format("Start to generate forced report:{0} for user:{1}", reportType.ReportId, user.Name));

                        var directoryInfo = new DirectoryInfo(new FilePathHelper(_configuration, user.Name).GetForcedReportsFolder());

                        FileInfo[] files = directoryInfo.GetFiles("*.report");

                        ReportProcessor reportProcessor = reportType.GetReportProcessor(_configuration, _logger);

                        bool result = reportProcessor.GenerateForcedReport(files.Select(x => x.FullName).ToList(), user, reportExecution);

                        if (result)
                        {
                            foreach (FileInfo fileInfo in files)
                            {
                                File.Delete(fileInfo.FullName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(string.Format("Error to generate force report:{0}", ex));
                    }
                }));

                threadReport.Start();
            }
        }

        private void LoadUserReports()
        {
            List<IUserConfiguration> reportUsers = _users.Where(x => x.Reports != null).ToList();

            foreach (IUserConfiguration user in reportUsers)
            {
                foreach (ReportTypeConfiguration reportType in user.Reports.ReportsList)
                {
                    ReportExecution reportExecution = reportType.GetReportExecution(user, null);
                    _reports.Add(reportExecution);
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

                    foreach (ReportExecution reportExecution in _reports)
                    {
                        if (DateTime.UtcNow >= reportExecution.NextRun)
                        {
                            try
                            {
                                IUserConfiguration user = _users.Where(x => x.Name == reportExecution.UserName).FirstOrDefault();

                                ReportTypeConfiguration reportType = user.Reports.ReportsList.Where(x => x.ReportId == reportExecution.ReportId).FirstOrDefault();

                                ReportProcessor reportProcessor = reportType.GetReportProcessor(_configuration, _logger);

                                reportProcessor.Process(user, reportExecution);

                                reportType.GetReportExecution(user, reportExecution);
                            }
                            catch (Exception e)
                            {
                                _logger.Error($"Error to find report processor for report:{reportExecution.ReportId} user:{reportExecution.UserName} -- {e}");
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
