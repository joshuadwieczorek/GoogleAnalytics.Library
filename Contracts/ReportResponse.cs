using System.Collections.Generic;
using Google.Apis.AnalyticsReporting.v4.Data;

namespace GoogleAnalytics.Library.Contracts
{
    public class ReportResponse
    {
        public long ViewId { get; set; }
        public List<Report> Reports { get; set; }
        public DateRange DateRange { get; set; }


        public ReportResponse()
        {
            Reports = new List<Report>();
        }
    }
}