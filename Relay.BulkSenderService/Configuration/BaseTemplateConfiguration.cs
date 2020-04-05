﻿using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public abstract class BaseTemplateConfiguration : ITemplateConfiguration
    {
        public List<string> DownloadFolders { get; set; }
        public string AttachmentsFolder { get; set; }
        public char FileNamePartSeparator { get; set; }
        public char FieldSeparator { get; set; }
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public bool HasHeaders { get; set; }
        public List<string> AllFileNameParts { get; set; }
        public List<string> AnyFileNameParts { get; set; }
        public List<string> SubFileNameParts { get; set; }
        public List<FieldConfiguration> Fields { get; set; }
        public bool AllowDuplicates { get; set; }
        public IPreProcessorConfiguration PreProcessor { get; set; }

        public abstract Processor GetProcessor(ILog logger, IConfiguration configuration);
    }
}
