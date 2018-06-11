﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Relay.BulkSenderService.Classes
{
    public class SqlHelper
    {
        private SqlConnection _sqlConnection;

        public SqlConnection Connection
        {
            get
            {
                if (_sqlConnection == null)
                {
                    _sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["RelayConnectionString"].ConnectionString);
                    _sqlConnection.Open();
                }
                return _sqlConnection;
            }
        }

        public void CloseConnection()
        {
            if (_sqlConnection != null)
            {
                _sqlConnection.Close();
            }
        }

        public List<DBStatusReportItem> GetResultsByDeliveryList(int userId, List<string> guidList)
        {
            var items = new List<DBStatusReportItem>();

            if (guidList.Count == 0)
            {
                return items;
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("Guid", typeof(string));

            foreach (string g in guidList)
            {
                DataRow row = dataTable.NewRow();
                row["Guid"] = g;
                dataTable.Rows.Add(row);
            }

            var command = new SqlCommand("BulkSender_GetDeliveriesReport", Connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter
            {
                ParameterName = "@Guids",
                SqlDbType = SqlDbType.Structured,
                Value = dataTable
            });

            command.Parameters.Add(new SqlParameter
            {
                ParameterName = "@UserId",
                SqlDbType = SqlDbType.Int,
                Value = userId
            });

            using (SqlDataReader sqlDataReader = command.ExecuteReader())
            {
                if (sqlDataReader.HasRows)
                {
                    while (sqlDataReader.Read())
                    {
                        var item = new DBStatusReportItem()
                        {
                            DeliveryId = Convert.ToInt32(sqlDataReader["Id"]),
                            CreatedAt = Convert.ToDateTime(sqlDataReader["CreatedAt"]),
                            Status = Convert.ToInt32(sqlDataReader["Status"]),
                            ClickEventsCount = Convert.ToInt32(sqlDataReader["ClickEventsCount"]),
                            OpenEventsCount = Convert.ToInt32(sqlDataReader["OpenEventsCount"]),
                            SentAt = Convert.ToDateTime(sqlDataReader["SentAt"]),
                            FromEmail = sqlDataReader["FromEmail"] != DBNull.Value ? Convert.ToString(sqlDataReader["FromEmail"]) : string.Empty,
                            FromName = sqlDataReader["FromName"] != DBNull.Value ? Convert.ToString(sqlDataReader["FromName"]) : string.Empty,
                            Subject = sqlDataReader["Subject"] != DBNull.Value ? Convert.ToString(sqlDataReader["Subject"]) : string.Empty,
                            MessageGuid = Convert.ToString(sqlDataReader["Guid"]),
                            Address = Convert.ToString(sqlDataReader["Address"]),
                            IsHard = Convert.ToBoolean(sqlDataReader["IsHard"]),
                            MailStatus = Convert.ToInt32(sqlDataReader["MailStatus"]),
                            OpenDate = Convert.ToDateTime(sqlDataReader["OpenDate"]),
                            ClickDate = Convert.ToDateTime(sqlDataReader["ClickDate"]),
                            BounceDate = Convert.ToDateTime(sqlDataReader["BounceDate"]),
                            Unsubscribed = Convert.ToBoolean(sqlDataReader["Unsubscribed"])
                        };
                        items.Add(item);
                    }
                }
            }
            return items;
        }

        public List<DBStatusReportItem> GetClicksByDeliveryList(int userId, List<string> guidList)
        {
            var items = new List<DBStatusReportItem>();

            if (guidList.Count == 0)
            {
                return items;
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("Guid", typeof(string));

            foreach (string g in guidList)
            {
                DataRow row = dataTable.NewRow();
                row["Guid"] = g;
                dataTable.Rows.Add(row);
            }

            var command = new SqlCommand("BulkSender_GetClicksReport", Connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter
            {
                ParameterName = "@Guids",
                SqlDbType = SqlDbType.Structured,
                Value = dataTable
            });

            command.Parameters.Add(new SqlParameter
            {
                ParameterName = "@UserId",
                SqlDbType = SqlDbType.Int,
                Value = userId
            });

            using (SqlDataReader sqlDataReader = command.ExecuteReader())
            {
                if (sqlDataReader.HasRows)
                {
                    while (sqlDataReader.Read())
                    {
                        var item = new DBStatusReportItem()
                        {
                            DeliveryId = Convert.ToInt32(sqlDataReader["Id"]),
                            CreatedAt = Convert.ToDateTime(sqlDataReader["CreatedAt"]),
                            Status = Convert.ToInt32(sqlDataReader["Status"]),
                            ClickEventsCount = Convert.ToInt32(sqlDataReader["ClickEventsCount"]),
                            OpenEventsCount = Convert.ToInt32(sqlDataReader["OpenEventsCount"]),
                            FromEmail = sqlDataReader["FromEmail"] != DBNull.Value ? Convert.ToString(sqlDataReader["FromEmail"]) : string.Empty,
                            FromName = sqlDataReader["FromName"] != DBNull.Value ? Convert.ToString(sqlDataReader["FromName"]) : string.Empty,
                            Subject = sqlDataReader["Subject"] != DBNull.Value ? Convert.ToString(sqlDataReader["Subject"]) : string.Empty,
                            MessageGuid = Convert.ToString(sqlDataReader["Guid"]),
                            Address = Convert.ToString(sqlDataReader["Address"]),
                            ClickDate = Convert.ToDateTime(sqlDataReader["ClickDate"]),
                            LinkUrl = Convert.ToString(sqlDataReader["Url"])
                        };
                        items.Add(item);
                    }
                }
            }
            return items;
        }

        public List<DBSummarizedReportItem> GetSummarizedByDate(int userId, DateTime startDate, DateTime endDate)
        {
            var items = new List<DBSummarizedReportItem>();

            var command = new SqlCommand("BulkSender_GetSumarizedByTemplateReport", Connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter
            {
                ParameterName = "@UserId",
                SqlDbType = SqlDbType.Int,
                Value = userId
            });

            command.Parameters.Add(new SqlParameter
            {
                ParameterName = "@StartDate",
                SqlDbType = SqlDbType.DateTime,
                Value = startDate
            });

            command.Parameters.Add(new SqlParameter
            {
                ParameterName = "@EndDate",
                SqlDbType = SqlDbType.DateTime,
                Value = endDate
            });

            using (SqlDataReader sqlDataReader = command.ExecuteReader())
            {
                if (sqlDataReader.HasRows)
                {
                    while (sqlDataReader.Read())
                    {
                        var item = new DBSummarizedReportItem()
                        {
                            TemplateId = Convert.ToInt32(sqlDataReader["Id"]),
                            TemplateName = Convert.ToString(sqlDataReader["Name"]),
                            TemplateGuid = Convert.ToString(sqlDataReader["Guid"]),
                            TemplateFromEmail = Convert.ToString(sqlDataReader["FromEmail"]),
                            TemplateFromName = Convert.ToString(sqlDataReader["FromName"]),
                            TemplateSubject = Convert.ToString(sqlDataReader["Subject"]),
                            TotalDeliveries = Convert.ToInt32(sqlDataReader["TotalSentCount"]),
                            TotalRetries = Convert.ToInt32(sqlDataReader["TotalRetriesCount"]),
                            TotalOpens = Convert.ToInt32(sqlDataReader["TotalOpensCount"]),
                            TotalUniqueOpens = Convert.ToInt32(sqlDataReader["UniqueOpensCount"]),
                            LastOpenDate = sqlDataReader["LastOpen"] != DBNull.Value ? Convert.ToDateTime(sqlDataReader["LastOpen"]) : DateTime.MinValue,
                            TotalClicks = Convert.ToInt32(sqlDataReader["TotalClicksCount"]),
                            TotalUniqueClicks = Convert.ToInt32(sqlDataReader["UniqueClicksCount"]),
                            LastClickDate = sqlDataReader["LastClick"] != DBNull.Value ? Convert.ToDateTime(sqlDataReader["LastClick"]) : DateTime.MinValue,
                            TotalUnsubscriptions = Convert.ToInt32(sqlDataReader["TotalUnsubscriptionsCount"]),
                            TotalHardBounces = Convert.ToInt32(sqlDataReader["HardBouncesCount"]),
                            TotalSoftBounces = Convert.ToInt32(sqlDataReader["SoftBouncesCount"])
                        };
                        items.Add(item);
                    }
                }
            }
            return items;
        }
    }

    public class DBStatusReportItem
    {
        public int DeliveryId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Status { get; set; }
        public int ClickEventsCount { get; set; }
        public int OpenEventsCount { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public string Subject { get; set; }
        public string MessageGuid { get; set; }
        public string Address { get; set; }
        public bool IsHard { get; set; }
        public int MailStatus { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime ClickDate { get; set; }
        public DateTime BounceDate { get; set; }
        public string LinkUrl { get; set; }
        public DateTime SentAt { get; set; }
        public bool Unsubscribed { get; set; }
    }

    public class DBSummarizedReportItem
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string TemplateGuid { get; set; }
        public string TemplateFromEmail { get; set; }
        public string TemplateFromName { get; set; }
        public string TemplateSubject { get; set; }
        public int TotalDeliveries { get; set; }
        public int TotalRetries { get; set; }
        public int TotalOpens { get; set; }
        public int TotalUniqueOpens { get; set; }
        public DateTime LastOpenDate { get; set; }
        public int TotalClicks { get; set; }
        public int TotalUniqueClicks { get; set; }
        public DateTime LastClickDate { get; set; }
        public int TotalUnsubscriptions { get; set; }
        public int TotalHardBounces { get; set; }
        public int TotalSoftBounces { get; set; }
    }
}
