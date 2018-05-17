using System.Collections.Generic;

namespace Relay.BulkSenderService.Reports
{
    public class ReportItem
    {
        private List<string> _values;

        public string ResultId { get; set; }

        public ReportItem()
        {
            _values = new List<string>();
        }

        public void AddValue(string value)
        {
            _values.Add(value);
        }

        public List<string> GetValues()
        {
            return _values;
        }

        public void AddValue(string value, int position)
        {
            if (position == -1)
            {
                return;
            }

            if (position >= 0 && position <= _values.Count)
            {
                _values.Insert(position, value);
            }
            else
            {
                _values.Add(value);
            }
        }
    }
}
