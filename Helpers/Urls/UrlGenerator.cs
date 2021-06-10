using AAG.Global.ExtensionMethods;

namespace GoogleAnalytics.Library.Helpers.Urls
{
    public static class UrlGenerator
    {
        /// <summary>
        /// Generate URL.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static string Generate(
              string host
            , string endpoint)
        {
            if (host.HasValue() && endpoint.HasValue())
                return $"{host.TrimEnd('/')}/{endpoint}";
            return string.Empty;
        }
    }
}