﻿using Relay.BulkSenderService.Configuration;
using Relay.BulkSenderService.Configuration.Alerts;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Processors.Errors
{
    public class AdminError : Error
    {
        public AdminError(IConfiguration configuration) : base(configuration)
        {
        }

        public override void Process()
        {
            var errorAlertTypeConfiguration = new ErrorAlertTypeConfiguration()
            {
                Subject = "BulkSender Error"
            };

            var alertConfiguration = new AlertConfiguration()
            {
                Emails = new List<string>() { "leve2support@makingsense.com" },
                AlertList = new List<IAlertTypeConfiguration>() { errorAlertTypeConfiguration }
            };

            SendErrorEmail(string.Empty, alertConfiguration);
        }

        protected override string GetBody()
        {
            return "Problems with bulksender and ftp connection. Please check the application log.";
        }
    }
}
