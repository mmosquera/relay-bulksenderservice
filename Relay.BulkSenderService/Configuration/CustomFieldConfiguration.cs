namespace Relay.BulkSenderService.Configuration
{
    public class CustomFieldConfiguration : FieldConfiguration
    {                
        public object GetValue(string data)
        {
            return $"XXXX-{data.Substring(data.Length - 4)}";
        }
    }
}
