namespace Relay.BulkSenderService.Configuration.Alerts
{
    public class EndAlertTypeConfiguration : IAlertTypeConfiguration
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }

        public IAlertTypeConfiguration Clone()
        {
            var endAlertTypeConfiguration = new EndAlertTypeConfiguration()
            {
                Name = this.Name,
                Subject = this.Subject,
                Message = this.Message
            };

            return endAlertTypeConfiguration;
        }
    }
}
