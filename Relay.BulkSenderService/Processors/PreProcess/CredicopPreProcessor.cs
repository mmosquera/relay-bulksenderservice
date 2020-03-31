using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public class CredicopPreProcessor : PreProcessor
    {
        private List<TemplateMapping> _mappings;

        public CredicopPreProcessor(ILog logger, IConfiguration configuration, List<TemplateMapping> mappings) : base(logger, configuration)
        {
            _mappings = mappings;
        }

        public override void ProcessFile(string fileName, IUserConfiguration userConfiguration)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            var mails = new List<Dictionary<string, string>>();

            Dictionary<string, string> mail = null;
            Dictionary<int, string> auxHeaders = null;
            Dictionary<int, string> auxValues = null;

            var headers = new List<string>() { "email", "name", "templateid", "replyto", "subject" };

            try
            {
                using (var sr = new StreamReader(fileName))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();

                        if (line.StartsWith("[MAIL", StringComparison.InvariantCultureIgnoreCase) && line.EndsWith("]"))
                        {
                            mail = new Dictionary<string, string>();
                            auxHeaders = new Dictionary<int, string>();
                            auxValues = new Dictionary<int, string>();

                            line = sr.ReadLine();
                        }

                        if (mail != null)
                        {
                            string[] pair = line.Split('=');

                            if (pair.Length > 1)
                            {
                                string key = pair[0].Trim();
                                string value = pair[1].Trim().Replace("\"", string.Empty);

                                switch (key)
                                {
                                    case "Template":
                                        string templateId = GetTemplateId(value);
                                        mail.Add("templateid", templateId);
                                        break;
                                    case "ToAddress":
                                        mail.Add("email", value);
                                        break;
                                    case "ReplyAddress":
                                        mail.Add("replyto", value);
                                        break;
                                    case "Subject":
                                        mail.Add("subject", value);
                                        break;
                                    default:
                                        if (key.Contains("TagName"))
                                        {
                                            int index = 0;
                                            int.TryParse(key.Replace("TagName", string.Empty), out index);
                                            auxHeaders.Add(index, value);

                                            if (!headers.Contains(value))
                                            {
                                                headers.Add(value);
                                            }
                                        }

                                        if (key.Contains("TagContent"))
                                        {
                                            int index = 0;
                                            int.TryParse(key.Replace("TagContent", string.Empty), out index);
                                            auxValues.Add(index, value);
                                        }
                                        break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(line) && mail != null)
                        {
                            foreach (int key in auxHeaders.Keys)
                            {
                                if (auxValues.ContainsKey(key))
                                {
                                    mail.Add(auxHeaders[key], auxValues[key]);
                                }
                            }

                            mails.Add(mail);

                            mail = null;
                            auxHeaders = null;
                            auxValues = null;
                        }
                    }
                }

                var sb = new StringBuilder();
                char separator = ';';

                sb.AppendLine(string.Join(separator.ToString(), headers));

                string[] values;

                foreach (var dMail in mails)
                {
                    values = new string[headers.Count];

                    for (int i = 0; i < headers.Count; i++)
                    {
                        if (dMail.ContainsKey(headers[i]))
                        {
                            values[i] = dMail[headers[i]];
                        }
                    }

                    sb.AppendLine(string.Join(separator.ToString(), values));
                }

                var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);
                string newFileName = $@"{filePathHelper.GetDownloadsFolder()}\{Path.GetFileNameWithoutExtension(fileName)}{Constants.EXTENSION_PROCESSING}";

                using (var sw = new StreamWriter(newFileName))
                {
                    sw.Write(sb.ToString());
                }

                File.Delete(fileName);
            }
            catch (Exception e)
            {
                _logger.Error($"ERROR CREDICOP PRE PROCESSOR: {e}");
            }
        }

        private string GetTemplateId(string name)
        {
            return _mappings.FirstOrDefault(x => x.TemplateName.Equals(name, StringComparison.InvariantCultureIgnoreCase)).TemplateId;
        }
    }
}
