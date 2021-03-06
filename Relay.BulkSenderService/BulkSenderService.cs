﻿using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors;
using System.ServiceProcess;
using System.Threading;

namespace Relay.BulkSenderService
{
    public partial class BulkSenderService : ServiceBase
    {
        private readonly ILog _logger;
        private readonly LocalMonitor _localMonitor;
        private readonly FtpMonitor _ftpMonitor;
        private readonly ReportGenerator _reportGenerator;
        private readonly CleanProcessor _cleanProcessor;
        private Thread _ftpMonitorThread;
        private Thread _localMonitorThread;
        private Thread _reportGeneratorThread;
        private Thread _cleanThread;

        public BulkSenderService(ILog logger, LocalMonitor localMonitor, FtpMonitor ftpMonitor, ReportGenerator reportGenerator, CleanProcessor cleanProcessor)
        {
            _logger = logger;
            _localMonitor = localMonitor;
            _ftpMonitor = ftpMonitor;
            _cleanProcessor = cleanProcessor;
            _reportGenerator = reportGenerator;
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _logger.Debug("Starting ftp monitor thread...");
            _ftpMonitorThread = new Thread(new ThreadStart(_ftpMonitor.ReadFtpFiles));
            _ftpMonitorThread.Start();

            _logger.Debug("Starting local monitor thread...");
            _localMonitorThread = new Thread(new ThreadStart(_localMonitor.ReadLocalFiles));
            _localMonitorThread.Start();

            _logger.Debug("Starting report generator thread...");
            _reportGeneratorThread = new Thread(new ThreadStart(_reportGenerator.Process));
            _reportGeneratorThread.Start();

            _logger.Debug("Starting clean processor thread...");
            _cleanThread = new Thread(new ThreadStart(_cleanProcessor.Process));
            _cleanThread.Start();
        }

        protected override void OnStop()
        {
            _logger.Debug("Stopping ftp monitor thread...");
            _ftpMonitorThread.Abort();

            _logger.Debug("Stopping local monitor thread...");
            _localMonitorThread.Abort();

            _logger.Debug("Stopping report generator thread...");
            _reportGeneratorThread.Abort();

            _logger.Debug("Stopping clean thread...");
            _cleanThread.Abort();
        }
    }
}
