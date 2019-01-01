namespace Relay.BulkSenderService.Configuration
{
	public class ReportFieldConfiguration
	{
		public string HeaderName { get; set; }
		public int Position { get; set; }
		public string NameInFile { get; set; }
		public string NameInDB { get; set; }
		public int PositionInFile { get; set; }

		public ReportFieldConfiguration Clone()
		{
			var reportFieldConfiguration = new ReportFieldConfiguration()
			{

				HeaderName = this.HeaderName,
				Position = this.Position,
				NameInFile = this.NameInFile,
				NameInDB = this.NameInDB,
				PositionInFile = this.PositionInFile
			};

			return reportFieldConfiguration;
		}
	}
}
