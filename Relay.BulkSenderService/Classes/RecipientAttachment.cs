using Newtonsoft.Json;

namespace Relay.BulkSenderService.Classes
{
    public class RecipientAttachment
    {
        [JsonProperty(PropertyName = "base64_content")]
        public string Base64String { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string FileName { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string FileType { get; set; }
    }
}
