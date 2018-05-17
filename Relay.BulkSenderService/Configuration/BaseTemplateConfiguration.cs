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

        public ITemplateConfiguration Clone()
        {
            ITemplateConfiguration templateConfiguration = (ITemplateConfiguration)Activator.CreateInstance(this.GetType());

            templateConfiguration.FileNamePartSeparator = this.FileNamePartSeparator;
            templateConfiguration.FieldSeparator = this.FieldSeparator;
            templateConfiguration.TemplateId = this.TemplateId;
            templateConfiguration.TemplateName = this.TemplateName;
            templateConfiguration.HasHeaders = this.HasHeaders;

            if (this.FileNameParts != null)
            {
                templateConfiguration.FileNameParts = new List<string>();
                foreach (string fileNamePart in this.FileNameParts)
                {
                    templateConfiguration.FileNameParts.Add(fileNamePart);
                }
            }

            if (this.Fields != null)
            {
                templateConfiguration.Fields = new List<FieldConfiguration>();
                foreach (FieldConfiguration fieldConfiguration in this.Fields)
                {
                    templateConfiguration.Fields.Add(fieldConfiguration.Clone());
                }
            }

            return templateConfiguration;
        }

        public abstract Processor GetProcessor(ILog logger, IConfiguration configuration);
    }
}
