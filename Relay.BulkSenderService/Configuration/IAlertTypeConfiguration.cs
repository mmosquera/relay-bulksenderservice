namespace Relay.BulkSenderService.Configuration
{
	public interface IAlertTypeConfiguration
	{
		string Name { get; set; }

		IAlertTypeConfiguration Clone();
	}
}
