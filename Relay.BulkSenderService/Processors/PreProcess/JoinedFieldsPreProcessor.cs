using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public class JoinedFieldsPreProcessor : ZipPreProcessor
    {
        public JoinedFieldsPreProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
        }

        public override void ProcessFile(string fileName, IUserConfiguration userConfiguration)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            try
            {
                string unzipFolder = GetUnzipFolder(fileName, userConfiguration);

                string dataFileName = $@"{unzipFolder}\{Path.GetFileNameWithoutExtension(fileName)}.txt";

                if (!File.Exists(dataFileName))
                {
                    return;
                }

                ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(dataFileName);

                var headers = new List<string>();

                var lines = new List<Dictionary<string, string>>();

                using (var streamReader = new StreamReader(dataFileName))
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

                        var fields = new Dictionary<string, string>();

                        for (int i = 0; i < lineArray.Length; i++)
                        {
                            FieldConfiguration field = templateConfiguration.Fields.Where(x => x.Position == i).FirstOrDefault();

                            if (field != null)
                            {
                                if (!field.IsJoined)
                                {
                                    if (!fields.ContainsKey(field.Name))
                                    {
                                        fields.Add(field.Name, lineArray[i]);
                                    }

                                    if (!headers.Contains(field.Name))
                                    {
                                        headers.Add(field.Name);
                                    }
                                }
                                else
                                {
                                    string[] joinedFields = lineArray[i].Split(field.JoinedFieldSeparator);

                                    foreach (string joinedField in joinedFields)
                                    {
                                        string[] pair = joinedField.Split(field.KeyValueSeparator);

                                        if (pair.Length > 1)
                                        {
                                            string key = pair[0];
                                            string value = pair[1];

                                            if (!fields.ContainsKey(key))
                                            {
                                                fields.Add(key, value);
                                            }

                                            if (!headers.Contains(key))
                                            {
                                                headers.Add(key);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        lines.Add(fields);
                    }
                }

                var stringBuilder = new StringBuilder();

                stringBuilder.AppendLine(string.Join(templateConfiguration.FieldSeparator.ToString(), headers));

                foreach (var dictionary in lines)
                {
                    string[] auxArray = new string[headers.Count];

                    for (int i = 0; i < headers.Count; i++)
                    {
                        string key = headers.ElementAt(i);

                        if (dictionary.ContainsKey(key))
                        {
                            auxArray[i] = dictionary[key];
                        }
                    }

                    stringBuilder.AppendLine(string.Join(templateConfiguration.FieldSeparator.ToString(), auxArray));
                }

                var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

                string newFileName = $@"{filePathHelper.GetDownloadsFolder()}\{Path.GetFileNameWithoutExtension(dataFileName)}{Constants.EXTENSION_PROCESSING}";

                using (var streamWriter = new StreamWriter(newFileName))
                {
                    streamWriter.Write(stringBuilder.ToString());
                }
            }
            catch (Exception e)
            {
                _logger.Error($"ERROR JOINED FIELDS PRE PROCESSOR: {e}");
            }
        }
    }
}
