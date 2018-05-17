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
        public ReportGenerator(ILog logger, IConfiguration configuration, IWatcher watcher) : base(logger, configuration, watcher)
        {

        }

        public void Process()
        {
            while (true)
            {
                try
                {
                    CheckConfigChanges();

                    List<IUserConfiguration> reportUsers = _users.Where(x => x.Reports != null).ToList();

                    foreach (IUserConfiguration user in reportUsers)
                    {
                        GenerateForcedReport(user);

                        foreach (ReportTypeConfiguration reportType in user.Reports.ReportsList)
                        {
                            ReportProcessor reportProcessor = reportType.GetReportProcessor(_configuration, _logger);

                            reportProcessor.Process(user);
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

        private void GenerateForcedReport(IUserConfiguration user)
        {
            var directoryInfo = new DirectoryInfo(new FilePathHelper(_configuration, user.Name).GetForcedReportsFolder());

            FileInfo[] files = directoryInfo.GetFiles("*.report");

            if (files.Count() > 0)
            {
                _logger.Debug($"Force report generation for use {user.Name}");

                foreach (ReportTypeConfiguration reportType in user.Reports.ReportsList)
                {
                    ReportProcessor reportProcessor = reportType.GetReportProcessor(_configuration, _logger);

                    if (reportProcessor.GenerateForcedReport(files.Select(x => x.FullName).ToList(), user))
                    {
                        break;
                    }
                }

                foreach (FileInfo fileInfo in files)
                {
                    File.Delete(fileInfo.FullName);
                }
            }
        }
    }
}
