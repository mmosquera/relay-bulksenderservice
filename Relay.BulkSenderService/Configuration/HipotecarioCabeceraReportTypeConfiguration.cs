﻿using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Reports;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Configuration
{
    public class HipotecarioCabeceraReportTypeConfiguration : ReportTypeConfiguration
    {
        public override ReportTypeConfiguration Clone()
        {
            var hipotecarioCabeceraReportTypeConfiguration = new HipotecarioCabeceraReportTypeConfiguration();

            hipotecarioCabeceraReportTypeConfiguration.Hour = this.Hour;
            hipotecarioCabeceraReportTypeConfiguration.DateFormat = this.DateFormat;

            if (this.Name != null)
            {
                hipotecarioCabeceraReportTypeConfiguration.Name = this.Name.Clone();
            }

            if (this.ReportFields != null)
            {
                hipotecarioCabeceraReportTypeConfiguration.ReportFields = new List<ReportFieldConfiguration>();
                foreach (ReportFieldConfiguration field in this.ReportFields)
                {
                    hipotecarioCabeceraReportTypeConfiguration.ReportFields.Add(field.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                hipotecarioCabeceraReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    hipotecarioCabeceraReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            if (this.ReportItems != null)
            {
                hipotecarioCabeceraReportTypeConfiguration.ReportItems = new List<ReportItemConfiguration>();
                foreach (ReportItemConfiguration reportItem in this.ReportItems)
                {
                    hipotecarioCabeceraReportTypeConfiguration.ReportItems.Add(reportItem.Clone());
                }
            }

            return hipotecarioCabeceraReportTypeConfiguration;
        }

        public override ReportProcessor GetReportProcessor(IConfiguration configuration, ILog logger)
        {
            return new HipotecarioCabeceraReportProcessor(logger, configuration, this);
        }
    }
}
