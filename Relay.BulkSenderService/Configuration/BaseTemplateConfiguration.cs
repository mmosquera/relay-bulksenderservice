using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Processors;
using System;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public abstract class BaseTemplateConfiguration : ITemplateConfiguration
    {
        public char FileNamePartSeparator { get; set; }
        public char FieldSeparator { get; set; }
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public bool HasHeaders { get; set; }
        public List<string> FileNameParts { get; set; }
        public List<FieldConfiguration> Fields { get; set; }
        public bool AllowDuplicates { get; set; }

        public abstract Processor GetProcessor(ILog logger, IConfiguration configuration);
    }
}
