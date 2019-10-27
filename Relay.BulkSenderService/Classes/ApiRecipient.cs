using Relay.BulkSenderService.Queues;
using System;
using System.Collections.Generic;
using System.IO;

namespace Relay.BulkSenderService.Classes
{
    public class ApiRecipient : Recipient, IBulkQueueMessage
    {
        public string TemplateId { get; set; }
        public Dictionary<string, object> Fields { get; set; }
        public string Key { get; set; }
        public List<RecipientAttachment> Attachments { get; set; }
        public string Message { get; set; }
        public DateTime EnqueueTime { get; set; }
        public DateTime DequeueTime { get; set; }

        public ApiRecipient()
        {
            HasError = false;
            ToEmail = null;
            ToName = null;
            FromEmail = null;
            FromName = null;
            ReplyToEmail = null;
            ReplyToName = null;
            Fields = new Dictionary<string, object>();
        }

        public void FillAttachments(List<string> files)
        {
            if (Attachments == null)
            {
                Attachments = new List<RecipientAttachment>();
            }

            foreach (string fileName in files)
            {
                byte[] bytesArray = File.ReadAllBytes(fileName);
                Attachments.Add(new RecipientAttachment()
                {
                    base64_content = Convert.ToBase64String(bytesArray),
                    filename = Path.GetFileName(fileName),
                    type = GetContentTypeByExtension(fileName),
                });
            }
        }

        private string GetContentTypeByExtension(string filename)
        {
            switch (Path.GetExtension(filename))
            {
                case ".zip": return "application/x-zip-compressed";
                case ".mp3": return "audio/mp3";
                case ".gif": return "image/gif";
                case ".jpg": return "image/jpeg";
                case ".png": return "image/png";
                case ".htm": return "text/html";
                case ".html": return "text/html";
                case ".txt": return "text/plain";
                case ".xml": return "text/xml";
                case ".pdf": return "application/pdf";
                default: return "application/octet-stream";
            }
        }
    }
}
