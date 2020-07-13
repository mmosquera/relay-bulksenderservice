using System;
using System.Collections.Generic;
using System.Text;

namespace Relay.BulkSenderService.Processors.Errors
{
    public abstract class Error : IError
    {
        protected DateTime _date;
        protected string _message;
        public Dictionary<string, string> _extras;

        public Error()
        {
            _date = DateTime.UtcNow;
            _extras = new Dictionary<string, string>();
        }

        public string GetDescription()
        {
            var description = new StringBuilder();

            description.AppendLine(_message);

            description.AppendLine($"Hour:{_date}");

            foreach (KeyValuePair<string, string> extra in _extras)
            {
                description.AppendLine($"{extra.Key}:{extra.Value}");
            }

            return description.ToString();
        }

        public void AddExtra(string name, string value)
        {
            if (!_extras.ContainsKey(name))
            {
                _extras.Add(name, value);
            }
        }
    }
}
