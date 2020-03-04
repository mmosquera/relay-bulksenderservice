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

        //TODO: para cada is... hacer un tipo nuevo y si necesita un campo se agrega
        public bool IsJoined { get; set; }
        public char JoinedFieldSeparator { get; set; }
        public char KeyValueSeparator { get; set; }
    }
}
