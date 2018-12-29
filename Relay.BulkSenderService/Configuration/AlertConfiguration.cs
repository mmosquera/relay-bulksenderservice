using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
	public class AlertConfiguration
	{
		public List<string> Emails { get; set; }
		public List<IAlertTypeConfiguration> AlertList { get; set; }

		public AlertConfiguration Clone()
		{
			var alertConfiguration = new AlertConfiguration();

			if (this.Emails != null)
			{
				alertConfiguration.Emails = new List<string>();

				foreach (string email in this.Emails)
				{
					alertConfiguration.Emails.Add(email);
				}
			}

			if (this.AlertList != null)
			{
				alertConfiguration.AlertList = new List<IAlertTypeConfiguration>();

				foreach (IAlertTypeConfiguration alertTypeConfiguration in this.AlertList)
				{
					alertConfiguration.AlertList.Add(alertTypeConfiguration.Clone());
				}
			}

			return alertConfiguration;
		}
	}
}
