using AAG.Global.ExtensionMethods;
using Database.GoogleAnalytics.Domain.dbo;
using System.Collections.Generic;
using System.Linq;

namespace GoogleAnalytics.Library.Helpers
{
    public static class AppSettings
    {
        #region "Standard stuff"

        /// <summary>
        /// Thread lock.
        /// </summary>
        private static readonly object _threadLock = new object();

        /// <summary>
        /// Application configurations.
        /// </summary>
        private static List<Configuration> _configurations { get; set; } = new List<Configuration>();


        /// <summary>
        /// Set configurations.
        /// </summary>
        /// <param name="configurations"></param>
        public static void Set(List<Configuration> configurations)
        {
            lock (_threadLock)
            {
                _configurations = configurations;
            }
        }


        /// <summary>
        /// Get configuration value.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetValue(string key)
        {
            lock (_threadLock)
            {
                return _configurations
                    .FirstOrDefault(c => c.ConfigurationName.Lower() == key.Lower())
                    ?.ConfigurationValue;
            }
        }

        #endregion "Standard stuff"

        #region "Properties"

        public static string PathScheduledReportsPickup
            => GetValue("Path.ScheduledReportsPickup");

        public static string PathManualReportsPickup
            => GetValue("Path.ManualReportsPickup");
        
        public static string PathManualReportsArchive
            => GetValue("Path.ManualReportsArchive");
        
        public static string PathManualReportsFailed
            => GetValue("Path.ManualReportsFailed");       

        public static bool ScheduledReportsEnabled
            => GetValue("ScheduledReportsEnabled").ToBool();

        public static bool ManualReportsEnabled
            => GetValue("ManualReportsEnabled").ToBool();

        public static int ScheduledReportsSimultaneousBatches
            => GetValue("ScheduledReports.SimultaneousBatches").ToInt(1);

        public static int ScheduledReportsQueueBatchSize
            => GetValue("ScheduledReports.QueueBatchSize").ToInt(1);

        public static int ScheduledReportsWaitTimeInSeconds
            => GetValue("ScheduledReports.WaitTimeInSeconds").ToInt(1);

        public static int ManualReportsSimultaneousBatches
            => GetValue("ManualReports.SimultaneousBatches").ToInt(1);

        public static int ManualReportsQueueBatchSize
            => GetValue("ManualReports.QueueBatchSize").ToInt(1);

        public static int ManualReportsWaitTimeInSeconds
            => GetValue("ManualReports.WaitTimeInSeconds").ToInt(1);

        public static int QueueGeneratorScheduledMilliseconds
            => GetValue("QueueGenerator.Scheduled.Milliseconds").ToInt(0);

        public static int QueueGeneratorScheduledSeconds
            => GetValue("QueueGenerator.Scheduled.Seconds").ToInt(0);

        public static int QueueGeneratorScheduledMinutes
            => GetValue("QueueGenerator.Scheduled.Minutes").ToInt(0);

        public static int QueueGeneratorScheduledHours
            => GetValue("QueueGenerator.Scheduled.Hours").ToInt(0);

        public static int QueueGeneratorScheduledDays
            => GetValue("QueueGenerator.Scheduled.Days").ToInt(0);

        public static int QueueGeneratorManualMilliseconds
            => GetValue("QueueGenerator.Manual.Milliseconds").ToInt(0);

        public static int QueueGeneratorManualSeconds
            => GetValue("QueueGenerator.Manual.Seconds").ToInt(0);

        public static int QueueGeneratorManualMinutes
            => GetValue("QueueGenerator.Manual.Minutes").ToInt(0);

        public static int QueueGeneratorManualHours
            => GetValue("QueueGenerator.Manual.Hours").ToInt(0);

        public static int QueueGeneratorManualDays
            => GetValue("QueueGenerator.Manual.Days").ToInt(0);

        public static int RetryPolicyMaxAttempts
            => GetValue("RetryPolicyMaxAttempts").ToInt(1);

        public static int RetryPolicyRetryDelayMilliseconds
            => GetValue("RetryPolicyRetryDelayMilliseconds").ToInt(1);

        public static string FileExtensionScheduledReportFiles
            => GetValue("FileExtension.ScheduledReportFiles");

        public static string FileExtensionManualReportFiles
            => GetValue("FileExtension.ManualReportFiles");

        public static int QueueLogProcessorBatchSize
            => GetValue("QueueLogProcessor.BatchSize").ToInt(1);

        public static int QueueLogProcessorWaitTimeInSeconds
            => GetValue("QueueLogProcessor.WaitTimeInSeconds").ToInt(1);

        #endregion "Properties"
    }
}