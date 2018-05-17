using System.IO;

namespace Relay.BulkSenderService.Classes
{
    public class ProcessResult
    {
        public ResulType Type { get; set; }
        public int ProcessedCount { get; set; }
        public int ErrorsCount { get; set; }
        public string ErrorFileName { get; set; }

        public ProcessResult()
        {
            ProcessedCount = 0;
            ErrorsCount = 0;
            Type = ResulType.PROCESS;
        }

        public void WriteError(string message)
        {
            if (!string.IsNullOrEmpty(ErrorFileName))
            {
                using (StreamWriter sw = new StreamWriter(ErrorFileName, true))
                {
                    sw.WriteLine(message);
                }
            }
        }
    }

    public enum ResulType
    {
        DOWNLOAD = 1,
        UNZIP = 2,
        LOGIN = 3,
        PROCESS = 4,
        REPEATED = 5
    }
}
