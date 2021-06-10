using Newtonsoft.Json;

namespace GoogleAnalytics.Library.Contracts
{
    public struct Metric
    {
        [JsonProperty("Name")]
        public string MetricName { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("Expression")]
        public string Expression { get; set; }
    }
}