using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.BulkSenderService.Reports
{
    public abstract class ReportBase
    {
        public enum DeliveryStatus
        {
            Queued = 0,
            Sent = 1,
            Rejected = 2,
            Retrying = 3,
            Invalid = 4,
            Dropped = 5
        }

        public enum MailStatus : byte
        {
            OK = 0,
            Invalid = 1,
            RecipientRejected = 2,
            TimeOut = 3,
            TransactionError = 4,
            ServerRejected = 5,
            MailRejected = 6,
            MXNotFound = 7,
            InvalidEmail = 8,
            DelayedBounce = 9
        }

        protected readonly ILog _logger;
        protected List<ReportItem> _items;
        protected readonly ReportTypeConfiguration _reportConfiguration;
        protected List<string> _headerList;
        protected string _reportFileName;
        protected string _dateFormat;

        public List<string> SourceFiles { get; set; }
        // TODO: remove separator from report use from config.
        public char Separator { get; set; }
        public string ReportPath { get; set; }
        public int ReportGMT { get; set; }
        public int UserId { get; set; }

        public ReportBase(ILog logger, ReportTypeConfiguration reportConfiguration)
        {
            _logger = logger;
            _reportConfiguration = reportConfiguration;
            _items = new List<ReportItem>();
            _headerList = new List<string>();
        }

        public string Generate()
        {
            try
            {
                FillItems();

                FillReport();

                Save();

                return GetReportFileName();
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected abstract void FillItems();

        protected abstract void Save();

        protected abstract void FillReport();

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

                if (!_headerList.Contains(header.HeaderName) && !header.HeaderName.Equals("*"))
                {
                    _headerList.Add(header.HeaderName);
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
                        if (!_headerList.Contains(header))
                        {
                            _headerList.Add(header);
                        }
                    }
                }
            }

            processedIndex = fileHeaders.IndexOf(Constants.HEADER_PROCESS_RESULT);
            resultIndex = fileHeaders.IndexOf(Constants.HEADER_MESSAGE_ID);

            return headers;
        }

        protected string GetReportFileName()
        {
            return _reportFileName;
        }

        protected void GetDataFromDB(List<ReportItem> items, string dateFormat)
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

                    List<DBReportItem> dbReportItemList = sqlHelper.GetResultsByDeliveryList(UserId, aux);
                    foreach (DBReportItem dbReportItem in dbReportItemList)
                    {
                        ReportItem item = items.FirstOrDefault(x => x.ResultId == dbReportItem.MessageGuid);
                        if (item != null)
                        {
                            string status;
                            string description;
                            GetStatusAndDescription(dbReportItem, out status, out description);

                            foreach (ReportFieldConfiguration reportField in _reportConfiguration.ReportFields.Where(x => !string.IsNullOrEmpty(x.NameInDB)))
                            {
                                switch (reportField.NameInDB)
                                {
                                    case "CreatedAt":
                                        item.AddValue(dbReportItem.CreatedAt.AddHours(ReportGMT).ToString(dateFormat), reportField.Position);
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
                                        item.AddValue(dbReportItem.OpenDate.AddHours(ReportGMT).ToString(dateFormat), reportField.Position);
                                        break;
                                    case "ClickDate":
                                        item.AddValue(dbReportItem.ClickDate.AddHours(ReportGMT).ToString(dateFormat), reportField.Position);
                                        break;
                                    case "BounceDate":
                                        item.AddValue(dbReportItem.BounceDate.AddHours(ReportGMT).ToString(dateFormat), reportField.Position);
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
        protected void GetStatusAndDescription(DBReportItem item, out string status, out string description)
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
    }
}
