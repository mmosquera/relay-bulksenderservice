namespace Relay.BulkSenderService.Configuration.Alerts
{
    public class ErrorAlertTypeConfiguration : IAlertTypeConfiguration
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }

        public IAlertTypeConfiguration Clone()
        {
            var errorAlertTypeConfiguration = new ErrorAlertTypeConfiguration()
            {
                Name = this.Name,
                Subject = this.Subject,
                Message = this.Message
            };

            return errorAlertTypeConfiguration;
        }
    }
}
