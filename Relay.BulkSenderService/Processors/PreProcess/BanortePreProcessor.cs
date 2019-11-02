using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public class BanortePreProcessor : BasicPreProcessor
    {
        public BanortePreProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration) { }

        protected override void DownloadAttachments(string fileName, IUserConfiguration userConfiguration)
        {
            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(fileName);

            if (templateConfiguration == null || !templateConfiguration.Fields.Any(x => x.IsAttachment))
            {
                return;
            }

            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(fileStream))
            {
                if (templateConfiguration.HasHeaders)
                {
                    reader.ReadLine();
                }

                string line;
                string[] fields;
                string attachentName;

                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    fields = line.Split(templateConfiguration.FieldSeparator);

                    if (fields.Length >= 4)
                    {
                        attachentName = $@"{fields[0]}-{fields[1]}-{fields[2]}-{fields[3]}.pdf";

                        GetAttachmentFile(attachentName, fileName, userConfiguration);
                    }
                }
            }
        }
    }
}
