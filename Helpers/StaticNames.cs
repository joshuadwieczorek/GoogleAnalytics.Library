using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleAnalytics.Library.Helpers
{
    public static class StaticNames
    {
        #region "Connection strings"
        public static string ConnectionAccounts = "Accounts";
        public static string ConnectionStringGoogleAnalytics = "GoogleAnalyticsReporting";
        #endregion

        #region "appsettings.json configurations"
        public static string ConfigAppSettingsServiceDelayInMinutes = "AppSettingsServiceDelayInMinutes";
        public static string ConfigAppSettingsUrl = "AppSettingsUrl";
        public static string MessageBrokerNameQueueLog = "MessageBrokerName.QueueLog";
        public static string MessageBrokerHost = "MessageBroker.Host";
        public static string MessageBrokerUser = "MessageBroker.User";
        public static string MessageBrokerPassword = "MessageBroker.Password";
        #endregion

        #region "Database"
        public static string DbProcConfigurationsRead = "[dbo].[ConfigurationsRead]";
        public static string DbProcQueueScheduledCreate = "[queue].[QueueScheduledCreate]";
        public static string DbProcQueueManualCreate = "[queue].[QueueManualCreate]";
        public static string DbProcQueueScheduledRead = "[queue].[QueueScheduledRead]";
        public static string DbProcQueueManualRead = "[queue].[QueueManualRead]";
        public static string DbProcQueueScheduledUpdate = "[queue].[QueueScheduledUpdate]";
        public static string DbProcQueueManualUpdate = "[queue].[QueueManualUpdate]";
        public static string DbProcQueueLog = "[queue].[QueueLog]";
        public static string DbTypeLogTableType = "[queue].[LogTableType]";
        #endregion

        #region "Task names"
        public static string TaskNameAppSettings = "AppSettingsTask";
        public static string TaskNameScheduledQueueGenerator = "ScheduledQueueGeneratorTask";
        public static string TaskNameManualQueueGenerator = "ManualQueueGeneratorTask";
        public static string TaskNameQueueLogProcessor = "QueueLogProcessorTask";
        public static string TaskNameQueueLogMessageBrokerReader = "QueueLogMessageBrokerReaderTask";
        public static string TaskNameProcessScheduledReports = "ProcessScheduledReports";
        public static string TaskNameProcessManualReports = "ProcessManualReports";
        #endregion
    }
}
