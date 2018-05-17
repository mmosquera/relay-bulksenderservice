using Newtonsoft.Json;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Classes
{
    public class Recipient
    {
        public string ToEmail { get; set; }
        public string ToName { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public string ReplyToEmail { get; set; }
        public string ReplyToName { get; set; }
        public string Subject { get; set; }
        public bool HasError { get; set; }
        public string ResultLine { get; set; }
        public List<RecipientAttachment> Attachments { get; set; }

        public void AddProcessedResult(string line, char separator, string message)
        {
            ResultLine = $"{line}{separator}{message}";
        }

        public void AddSentResult(char separator, string message)
        {
            ResultLine += $"{separator}{message}";
        }
    }

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
