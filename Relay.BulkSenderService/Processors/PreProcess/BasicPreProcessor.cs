using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
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

            try
            {
                File.Move(fileName, newFileName);
            }
            catch (Exception e)
            {
                _logger.Error($"ERROR BASIC PRE PROCESSOR: {e}");
            }
        }
    }
}
