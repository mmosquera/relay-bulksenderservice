using System;
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

        public List<DBReportItem> GetResultsByDeliveryList(int userid, List<string> guidList)
        {
            var items = new List<DBReportItem>();

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
                Value = userid
            });

            using (SqlDataReader sqlDataReader = command.ExecuteReader())
            {
                if (sqlDataReader.HasRows)
                {
                    while (sqlDataReader.Read())
                    {
                        var item = new DBReportItem()
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
                            IsHard = Convert.ToBoolean(sqlDataReader["IsHard"]),
                            MailStatus = Convert.ToInt32(sqlDataReader["MailStatus"]),
                            OpenDate = Convert.ToDateTime(sqlDataReader["OpenDate"]),
                            ClickDate = Convert.ToDateTime(sqlDataReader["ClickDate"]),
                            BounceDate = Convert.ToDateTime(sqlDataReader["BounceDate"])
                        };
                        items.Add(item);
                    }
                }
            }
            return items;
        }
    }

    public class DBReportItem
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
    }
}
