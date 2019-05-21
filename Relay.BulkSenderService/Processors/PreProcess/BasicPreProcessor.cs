using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System.IO;

namespace Relay.BulkSenderService.Processors.PreProcess
{
    public class BasicPreProcessor : PreProcessor
    {
        public BasicPreProcessor(ILog logger, IConfiguration configuration) : base(logger, configuration)
        {
        }

        public override void ProcessFile(string fileName, string userName)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            string newFileName = fileName.Replace(Path.GetExtension(fileName), ".processing");

            File.Move(fileName, newFileName);
        }
    }
}
