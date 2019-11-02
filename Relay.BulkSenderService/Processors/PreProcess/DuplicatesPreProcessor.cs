using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public class DuplicatesPreProcessor : PreProcessor
    {

        public DuplicatesPreProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration) { }

        public override void ProcessFile(string fileName, IUserConfiguration userConfiguration)
        {
            string processedFolder = new FilePathHelper(_configuration, userConfiguration.Name).GetProcessedFilesFolder();

            var directory = new DirectoryInfo(processedFolder);

            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(fileName);

            List<int> indexes = templateConfiguration.Fields.Where(x => x.IsKey).Select(x => x.Position).ToList();

            string name = Path.GetFileNameWithoutExtension(fileName);
            name = name.Remove(name.LastIndexOf('_'));

            IEnumerable<string> files = directory.GetFiles()
                .Where(x => x.Name.Contains(name)
                && x.FullName != fileName)
                .Select(x => x.FullName);

            var processedKeys = new HashSet<string>();

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

                        string key = GetHashKey(lineArray, indexes);

                        if (!processedKeys.Contains(key))
                        {
                            processedKeys.Add(key);
                        }
                    }
                }
            }

            var stringBuilder = new StringBuilder();

            using (var sr = new StreamReader(fileName))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string[] lineArray = line.Split(templateConfiguration.FieldSeparator);

                    string hashKey = GetHashKey(lineArray, indexes);

                    if (!processedKeys.Contains(hashKey))
                    {
                        stringBuilder.AppendLine(line);

                        processedKeys.Add(hashKey);
                    }
                }
            }

            string newFileName = $"{ Path.GetFileNameWithoutExtension(fileName)}.processing";

            using (var streamWriter = new StreamWriter(newFileName))
            {
                streamWriter.Write(stringBuilder);
            }

            File.Delete(fileName);
        }

        private string GetHashKey(string[] lineArray, List<int> indexes)
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
