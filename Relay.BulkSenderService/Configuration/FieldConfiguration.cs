namespace Relay.BulkSenderService.Configuration
{
    public class FieldConfiguration
    {
        public string Name { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }
        public string Type { get; set; }
        public bool IsBasic { get; set; }
        public bool IsKey { get; set; }
        public bool IsForList { get; set; }
        public bool IsAttachment { get; set; }

        public FieldConfiguration Clone()
        {
            var fieldConfiguration = new FieldConfiguration();

            fieldConfiguration.Name = this.Name;
            fieldConfiguration.Position = this.Position;
            fieldConfiguration.Length = this.Length;
            fieldConfiguration.Type = this.Type;
            fieldConfiguration.IsBasic = this.IsBasic;
            fieldConfiguration.IsKey = this.IsKey;
            fieldConfiguration.IsForList = this.IsForList;
            fieldConfiguration.IsAttachment = this.IsAttachment;

            return fieldConfiguration;
        }
    }
}
