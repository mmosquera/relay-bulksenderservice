using System.IO;

namespace Relay.BulkSenderService.Classes
{
    class FileWriter
    {
        private readonly string filePath;
        private readonly object locker;

        public FileWriter(string filePath)
        {
            this.filePath = filePath;
            locker = new object();
        }

        public void WriteLine(string text)
        {
            lock (locker)
            {
                using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.WriteLine(text);
                }
            }
        }
    }
}
