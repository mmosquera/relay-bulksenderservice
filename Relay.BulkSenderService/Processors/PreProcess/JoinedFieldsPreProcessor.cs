using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public class JoinedFieldsPreProcessor : PreProcessor
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

            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(fileName);

            var headers = new List<string>();

            var lines = new List<Dictionary<string, string>>();

            using (var streamReader = new StreamReader(fileName))
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
                            if (!field.IsBasic && fields.ContainsKey(field.Name)) //aca pregunto si esta joineado
                            {
                                fields.Add(field.Name, lineArray[i]);

                                if (!headers.Contains(field.Name))
                                {
                                    headers.Add(field.Name);
                                }
                            }
                            else
                            {
                                string[] joinedFields = lineArray[i].Split(';');

                                foreach (string joinedField in joinedFields)
                                {
                                    string key = joinedField.Split('=')[0];
                                    string value = joinedField.Split('=')[1];

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

                    lines.Add(fields);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Join("|", headers));

            foreach (var d in lines)
            {
                string[] auxArray = new string[headers.Count];
                for (int i = 0; i < headers.Count; i++)
                {
                    string key = headers.ElementAt(i);
                    if (d.ContainsKey(key))
                    {
                        auxArray[i] = d[key];
                    }
                }
                sb.AppendLine(string.Join("|", auxArray));
            }

            string newFileName = fileName.Replace(Path.GetExtension(fileName), Constants.EXTENSION_PROCESSING);

            File.Move(fileName, newFileName);
        }
    }

}
