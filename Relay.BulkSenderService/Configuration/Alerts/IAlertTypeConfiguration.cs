namespace Relay.BulkSenderService.Configuration.Alerts
{
    public interface IAlertTypeConfiguration
    {
        string Name { get; set; }

        IAlertTypeConfiguration Clone();
    }
}
