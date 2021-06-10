using Newtonsoft.Json;

namespace GoogleAnalytics.Library.Contracts
{
    public struct Dimension
    {
        [JsonProperty("Name")]
        public string DimensionName { get; set; }
    }
}