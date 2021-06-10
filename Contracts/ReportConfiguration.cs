using System.Collections.Generic;
using Newtonsoft.Json;
using AAG.Global.Contracts;

namespace GoogleAnalytics.Library.Contracts
{
    public class ReportConfiguration
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Dimensions")]
        public List<Dimension> Dimensions { get; set; }

        [JsonProperty("Metrics")]
        public List<Metric> Metrics { get; set; }

        [JsonProperty("Filter")]
        public string Filter { get; set; }

        [JsonProperty("Schedule")]
        public AAG.Global.Enums.Schedule Schedule { get; set; }

        [JsonProperty("DatabaseTable")]
        public string DatabaseTable { get; set; }

        [JsonProperty("DateRange")]
        public AAG.Global.Contracts.DateRange? DateRange { get; set; }
    }
}