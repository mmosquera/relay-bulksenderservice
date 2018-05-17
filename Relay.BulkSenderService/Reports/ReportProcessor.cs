using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System.Collections.Generic;
using System.IO;

namespace Relay.BulkSenderService.Reports
{
    public abstract class ReportProcessor
    {
        protected readonly ILog _logger;
        protected readonly IConfiguration _configuration;
        protected readonly ReportTypeConfiguration _reportTypeConfiguration;

        public ReportProcessor(ILog logger, IConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration)
        {
            _logger = logger;
            _configuration = configuration;
            _reportTypeConfiguration = reportTypeConfiguration;
        }

        /// <summary>
        /// Retorna la lista de archivos para generarle los reportes necesarios.
        /// </summary>
        /// <param name="user">Configuracion del usuario.</param>
        /// <returns></returns>
        protected abstract List<string> GetFilesToProcess(IUserConfiguration user);

        /// <summary>
        /// Procesa los arhivos generando el reporte correspondiente.
        /// </summary>
        /// <param name="files">Lista de archivos para generar reporte.</param>
        /// <param name="user">Confuracion del usuario.</param>
        protected abstract void ProcessFilesForReports(List<string> files, IUserConfiguration user);

        public abstract bool GenerateForcedReport(List<string> files, IUserConfiguration user);

        protected void UploadFileToFtp(string fileName, string ftpFolder, IFtpHelper ftpHelper)
        {
            if (File.Exists(fileName) && !string.IsNullOrEmpty(ftpFolder))
            {
                string ftpFileName = $@"{ftpFolder}/{Path.GetFileName(fileName)}";

                _logger.Debug($"Upload file {ftpFileName} to ftp.");

                ftpHelper.UploadFileAsync(fileName, ftpFileName);
            }
        }

        public void Process(IUserConfiguration user)
        {
            List<string> files = GetFilesToProcess(user);

            ProcessFilesForReports(files, user);
        }

        protected List<string> FilterFilesByTemplate(List<string> files, IUserConfiguration user)
        {
            var filteredFiles = new List<string>();
            foreach (string file in files)
            {
                ITemplateConfiguration templateConfiguration = ((UserApiConfiguration)user).GetTemplateConfiguration(file);

                if (templateConfiguration != null && _reportTypeConfiguration.Templates.Contains(templateConfiguration.TemplateName))
                {
                    filteredFiles.Add(file);
                }
            }

            return filteredFiles;
        }
    }
}
