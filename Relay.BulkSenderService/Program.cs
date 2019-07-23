using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Processors;
using Relay.BulkSenderService.Processors.PreProcess;
using System.ServiceProcess;
using Unity;

namespace Relay.BulkSenderService
{
    static class Program
    {
        private static UnityContainer _container;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            Configure();

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                _container.Resolve<BulkSenderService>()
            };
            ServiceBase.Run(ServicesToRun);

            #region To run it as a console app:
            //FtpMonitor fm = _container.Resolve<FtpMonitor>();
            //LocalMonitor lm = _container.Resolve<LocalMonitor>();
            //ReportGenerator rg = _container.Resolve<ReportGenerator>();
            //PreProcessWorker pp = _container.Resolve<PreProcessWorker>();
            //CleanProcessor cp = _container.Resolve<CleanProcessor>();

            //fm.ReadFtpFiles();            
            //lm.ReadLocalFiles();
            //rg.Process();            
            //cp.Process();
            //pp.Process();
            #endregion
        }
        static void Configure()
        {
            _container = new UnityContainer();

            _container.RegisterType<Processor, SMTPProcessor>();
            _container.RegisterType<Processor, APIProcessor>();
            _container.RegisterType<ILogManager, LogManager>();
            _container.RegisterType<IConfiguration, AppConfiguration>();
            _container.RegisterType<BaseWorker, LocalMonitor>();
            _container.RegisterType<BaseWorker, FtpMonitor>();
            _container.RegisterType<BaseWorker, ReportGenerator>();
            _container.RegisterType<BaseWorker, CleanProcessor>();
            _container.RegisterType<BaseWorker, PreProcessWorker>();

            var logManager = _container.Resolve<ILogManager>();
            _container.RegisterInstance<ILog>(logManager.GetLogger("BulkSenderService"));
        }
    }
}
