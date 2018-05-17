namespace Relay.BulkSenderService.Configuration
{
    public class ErrorConfiguration
    {
        public string Folder { get; set; }
        public IReportName Name { get; set; }

        public ErrorConfiguration Clone()
        {
            var errorConfiguration = new ErrorConfiguration();

            errorConfiguration.Folder = this.Folder;
            if (this.Name != null)
            {
                errorConfiguration.Name = this.Name.Clone();
            }

            return errorConfiguration;
        }
    }
}
