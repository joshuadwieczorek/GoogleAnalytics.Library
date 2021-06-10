using System;

namespace GoogleAnalytics.Library.Common
{
    public static class ApplicationStatistics
    {
        public static DateTime? AppSettingsLastLoadedAt { get; set; } = null;
        public static bool AppSettingsSuccessfullyLoaded { get; set; } = false;
        public static string AppSettingsLoadErrorMessage { get; set; } = string.Empty;
    }
}