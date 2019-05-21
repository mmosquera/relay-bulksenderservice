﻿namespace Relay.BulkSenderService.Configuration.Alerts
{
    public class ReportAlertTypeConfiguration : IAlertTypeConfiguration
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }

        public IAlertTypeConfiguration Clone()
        {
            var reportAlertTypeConfiguration = new ReportAlertTypeConfiguration()
            {
                Name = this.Name,
                Subject = this.Subject,
                Message = this.Message
            };

            return reportAlertTypeConfiguration;
        }
    }
}