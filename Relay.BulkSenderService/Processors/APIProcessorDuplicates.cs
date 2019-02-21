using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.BulkSenderService.Processors
{
    public class APIProcessorDuplicates : APIProcessor
    {
        private HashSet<string> processedKeys;
        private List<int> indexes;

        public APIProcessorDuplicates(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
            processedKeys = new HashSet<string>();
        }

        protected override void CustomProcessForFile(string fileName, string userName, ITemplateConfiguration templateConfiguration)
        {
            string processedFolder = new FilePathHelper(_configuration, userName).GetProcessedFilesFolder();

            var directory = new DirectoryInfo(processedFolder);

            indexes = templateConfiguration.Fields.Where(x => x.IsKey).Select(x => x.Position).ToList();

            string name = Path.GetFileNameWithoutExtension(fileName);
            name = name.Remove(name.LastIndexOf('_'));

            IEnumerable<string> files = directory.GetFiles()
                .Where(x => x.Name.Contains(name)
                && x.FullName != fileName)
                .Select(x => x.FullName);

            foreach (string file in files)
            {
                using (var streamReader = new StreamReader(file))
                {
                    if (templateConfiguration.HasHeaders)
                    {
                        streamReader.ReadLine();
                    }

                    while (!streamReader.EndOfStream)
                    {
                        string line = streamReader.ReadLine();

                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }

                        string[] lineArray = line.Split(templateConfiguration.FieldSeparator);

                        string key = GetHashKey(lineArray);

                        if (!processedKeys.Contains(key))
                        {
                            processedKeys.Add(key);
                        }
                    }
                }
            }
        }

        protected override void CustomRecipientValidations(ApiRecipient recipient, string[] recipientArray, string line, char fielSeparator, ProcessResult result)
        {
            if (recipient.HasError)
            {
                return;
            }

            string key = GetHashKey(recipientArray);

            if (!processedKeys.Contains(key))
            {
                processedKeys.Add(key);
            }
            else
            {
                string message = "The recipient is already processed.";
                recipient.HasError = true;
                recipient.ResultLine = $"{line}{fielSeparator}{message}";
                string errorMessage = $"{DateTime.UtcNow}:{message} proccesing line {line}";
                result.WriteError(errorMessage);
                result.ErrorsCount++;
            }
        }

        protected override string GetBody(string file, IUserConfiguration user, ProcessResult result)
        {
            var templateGenerator = new TemplateGenerator();
            templateGenerator.AddItem(Path.GetFileNameWithoutExtension(file), user.GetUserDateTime().DateTime.ToString(), result.ProcessedCount.ToString(), result.ErrorsCount.ToString());

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

        private string GetHashKey(string[] lineArray)
        {
            var values = new List<string>();

            foreach (int index in indexes)
            {
                values.Add(lineArray[index]);
            }

            return string.Join("|", values);
        }
    }
}
