using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Classes.Enums;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        protected abstract bool IsTimeToRun(IUserConfiguration user);

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
            if (IsTimeToRun(user))
            {
                List<string> files = GetFilesToProcess(user);

                ProcessFilesForReports(files, user);
            }
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

        protected void GetDataFromDB(List<ReportItem> items, string dateFormat, int userId, int reportGMT)
        {
            List<string> guids = items.Select(it => it.ResultId).Distinct().ToList();

            var sqlHelper = new SqlHelper();

            try
            {
                int i = 0;
                while (i < guids.Count)
                {
                    // TODO use skip take from linq.
                    var aux = new List<string>();
                    for (int count = 0; i < guids.Count && count < 1000; count++)
                    {
                        aux.Add(guids[i]);
                        i++;
                    }

                    List<DBStatusReportItem> dbReportItemList = sqlHelper.GetResultsByDeliveryList(userId, aux);
                    foreach (DBStatusReportItem dbReportItem in dbReportItemList)
                    {
                        ReportItem item = items.FirstOrDefault(x => x.ResultId == dbReportItem.MessageGuid);
                        if (item != null)
                        {
                            string status;
                            string description;
                            GetStatusAndDescription(dbReportItem, out status, out description);

                            foreach (ReportFieldConfiguration reportField in _reportTypeConfiguration.ReportFields.Where(x => !string.IsNullOrEmpty(x.NameInDB)))
                            {
                                switch (reportField.NameInDB)
                                {
                                    case "CreatedAt":
                                        item.AddValue(dbReportItem.CreatedAt.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                                        break;
                                    case "Status":
                                        item.AddValue(status, reportField.Position);
                                        break;
                                    case "Description":
                                        item.AddValue(description, reportField.Position);
                                        break;
                                    case "ClickEventsCount":
                                        item.AddValue(dbReportItem.ClickEventsCount.ToString(), reportField.Position);
                                        break;
                                    case "OpenEventsCount":
                                        item.AddValue(dbReportItem.OpenEventsCount.ToString(), reportField.Position);
                                        break;
                                    case "Subject":
                                        item.AddValue(dbReportItem.Subject, reportField.Position);
                                        break;
                                    case "FromEmail":
                                        item.AddValue(dbReportItem.FromEmail, reportField.Position);
                                        break;
                                    case "FromName":
                                        item.AddValue(dbReportItem.FromName, reportField.Position);
                                        break;
                                    case "Address":
                                        item.AddValue(dbReportItem.Address, reportField.Position);
                                        break;
                                    case "OpenDate":
                                        item.AddValue(dbReportItem.OpenDate.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                                        break;
                                    case "ClickDate":
                                        item.AddValue(dbReportItem.ClickDate.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                                        break;
                                    case "BounceDate":
                                        item.AddValue(dbReportItem.BounceDate.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                                        break;
                                }
                            }
                        }
                    }

                    aux.Clear();
                }

                sqlHelper.CloseConnection();
            }
            catch (Exception e)
            {
                _logger.Error($"Error on get data from DB {e}");
                throw;
            }
        }

        // TODO: internationalize messages.
        protected void GetStatusAndDescription(DBStatusReportItem item, out string status, out string description)
        {
            description = string.Empty;
            status = string.Empty;

            switch (item.Status)
            {
                case (int)DeliveryStatus.Queued:
                    status = "Encolado";
                    description = "Encolado";
                    break;

                case (int)DeliveryStatus.Sent:
                    status = "Enviado";
                    if (item.OpenEventsCount > 0)
                    {
                        description = "Abierto";
                    }
                    else
                    {
                        description = "No abierto";
                    }
                    break;
                case (int)DeliveryStatus.Rejected:
                    status = "Mail rechazado";
                    break;
                case (int)DeliveryStatus.Invalid:
                    status = "Mail inválido";
                    break;
                case (int)DeliveryStatus.Retrying:
                    status = "Reintentando";
                    description = "Reintentando";
                    break;
                case (int)DeliveryStatus.Dropped:
                    status = "Descartado";
                    description = "Incluido en blacklist";
                    break;
            }

            if (item.Status == (int)DeliveryStatus.Rejected || item.Status == (int)DeliveryStatus.Invalid)
            {
                description = item.IsHard ? "Rebote Hard" : "Rebote soft";

                switch (item.MailStatus)
                {
                    case (int)MailStatus.Invalid:
                        description += " - Inválido";
                        break;
                    case (int)MailStatus.RecipientRejected:
                        description += " - Destinatario rechazado";
                        break;
                    case (int)MailStatus.TimeOut:
                        description += " - Time out";
                        break;
                    case (int)MailStatus.TransactionError:
                        description += " - Error de transacción";
                        break;
                    case (int)MailStatus.ServerRejected:
                        description += " - Servidor rechazado";
                        break;
                    case (int)MailStatus.MailRejected:
                        description += " - Mail rechazado";
                        break;
                    case (int)MailStatus.MXNotFound:
                        description += " - MX no encontrado";
                        break;
                    case (int)MailStatus.InvalidEmail:
                        description += " - Mail inválido";
                        break;
                }
            }
        }

        protected List<string> GetHeadersList(List<ReportFieldConfiguration> reportHeaders, List<string> fileHeaders = null)
        {
            var headersList = new List<string>();

            foreach (ReportFieldConfiguration header in reportHeaders)
            {
                if (!headersList.Contains(header.HeaderName) && !header.HeaderName.Equals("*"))
                {
                    headersList.Add(header.HeaderName);
                }
            }

            if (reportHeaders.Exists(x => x.HeaderName.Equals("*")) && fileHeaders != null)
            {
                string header;
                for (int i = 0; i < fileHeaders.Count; i++)
                {
                    header = fileHeaders[i];
                    if (header != Constants.HEADER_PROCESS_RESULT
                        && header != Constants.HEADER_MESSAGE_ID
                        && header != Constants.HEADER_DELIVERY_RESULT
                        && header != Constants.HEADER_DELIVERY_LINK
                        && !reportHeaders.Exists(x => x.NameInFile == header))
                    {
                        if (!headersList.Contains(header))
                        {
                            headersList.Add(header);
                        }
                    }
                }
            }

            return headersList;
        }

        protected Dictionary<string, int> GetHeadersIndexes(List<ReportFieldConfiguration> reportHeaders, List<string> fileHeaders, out int processedIndex, out int resultIndex)
        {
            var headers = new Dictionary<string, int>();

            foreach (ReportFieldConfiguration header in reportHeaders)
            {
                int index = fileHeaders.IndexOf(header.NameInFile);
                if (index != -1 && !headers.ContainsKey(header.NameInFile))
                {
                    headers.Add(header.NameInFile, index);
                }
            }

            if (reportHeaders.Exists(x => x.HeaderName.Equals("*")))
            {
                string header;
                for (int i = 0; i < fileHeaders.Count; i++)
                {
                    header = fileHeaders[i];
                    if (header != Constants.HEADER_PROCESS_RESULT
                        && header != Constants.HEADER_MESSAGE_ID
                        && header != Constants.HEADER_DELIVERY_RESULT
                        && header != Constants.HEADER_DELIVERY_LINK
                        && !headers.ContainsKey(header)
                        && !reportHeaders.Exists(x => x.NameInFile == header))
                    {
                        headers.Add(header, i);
                    }
                }
            }

            processedIndex = fileHeaders.IndexOf(Constants.HEADER_PROCESS_RESULT);
            resultIndex = fileHeaders.IndexOf(Constants.HEADER_MESSAGE_ID);

            return headers;
        }
    }
}
