﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Relay.BulkSenderService.Classes
{
    public class ApiRecipient : Recipient
    {
        public string TemplateId { get; set; }
        public Dictionary<string, object> Fields { get; set; }
        public string Key { get; set; }

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
            Attachments = new List<RecipientAttachment>();

            foreach (string fileName in files)
            {
                byte[] bytesArray = File.ReadAllBytes(fileName);
                Attachments.Add(new RecipientAttachment()
                {
                    Base64String = Convert.ToBase64String(bytesArray),
                    FileName = Path.GetFileName(fileName),
                    FileType = GetContentTypeByExtension(fileName),
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
