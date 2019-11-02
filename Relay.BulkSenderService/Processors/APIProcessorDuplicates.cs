using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Processors
{
    public class APIProcessorDuplicates : APIProcessor
    {
        public APIProcessorDuplicates(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {

        }

        protected override string GetBody(string file, IUserConfiguration user, int processedCount, int errorsCount)
        {
            var templateGenerator = new TemplateGenerator();
            templateGenerator.AddItem(Path.GetFileNameWithoutExtension(file), user.GetUserDateTime().DateTime.ToString(), processedCount.ToString(), errorsCount.ToString());

            return templateGenerator.GenerateHtml();
        }

        protected override List<string> GetAttachments(string file, string userName)
        {
            var attchments = new List<string>();

            string resultsFolder = new FilePathHelper(_configuration, userName).GetResultsFilesFolder();

            string name = $@"{Path.GetFileNameWithoutExtension(file)}_ERR";

            var directoryInfo = new DirectoryInfo(resultsFolder);

            return directoryInfo.GetFiles().Where(x => x.Name.Contains(name)).Select(x => x.FullName).ToList();
        }
    }
}
