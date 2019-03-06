namespace Relay.BulkSenderService.Configuration.Alerts
{
    public class StartAlertTypeConfiguration : IAlertTypeConfiguration
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }

        public IAlertTypeConfiguration Clone()
        {
            var startAlertTypeConfiguration = new StartAlertTypeConfiguration()
            {
                Name = this.Name,
                Subject = this.Subject,
                Message = this.Message
            };

            return startAlertTypeConfiguration;
        }
    }
}
