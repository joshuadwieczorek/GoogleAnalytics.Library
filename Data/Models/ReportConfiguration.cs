using System;
using System.Collections.Generic;
using GoogleAnalytics.Library.Contracts;
using Database.Accounts.Domain.configurations;

namespace GoogleAnalytics.Library.Data.Models
{
    public class ReportConfiguration
    {
        public string ReportName { get; set; }
        public int GoogleId { get; set; }
        public long ViewId { get; set; }        
        public string Filter { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseTableName { get; set; }
        public List<GoogleVdpUrlPattern> VdpUrlPatterns { get; set; }  
        public string Credentials { get; set; }
        public List<Dimension> Dimensions { get; set; }
        public List<Metric> Metrics { get; set; }
        public DateTime ReportDateStart { get; set; }
        public DateTime ReportDateEnd { get; set; }        
    }
}