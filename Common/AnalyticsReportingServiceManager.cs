using Google.Apis.AnalyticsReporting.v4;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace GoogleAnalytics.Library.Common
{
    internal static class AnalyticsReportingServiceManager
    {
        // Thread lock object.
        private static readonly object _threadLock = new object();
        
        // All services.
        private static ConcurrentDictionary<long, AnalyticsReportingService> services = new ConcurrentDictionary<long, AnalyticsReportingService>();


        /// <summary>
        /// And or find and return service.
        /// </summary>
        /// <param name="googleAccount"></param>
        /// <returns></returns>
        internal static AnalyticsReportingService AddAndOrFind(Database.Accounts.Domain.accounts.Google googleAccount)
        {
            lock (_threadLock)
            {
                if (services.TryGetValue(googleAccount.ViewId, out AnalyticsReportingService service))
                    return service;

                var newService = new AnalyticsReportingService(AnalyticsServiceInitializer(googleAccount.Credentials));
                services.TryAdd(googleAccount.ViewId, newService);
                return newService;
            }
        }


        /// <summary>
        /// Generate service initializer.
        /// </summary>
        /// <param name="credentialString"></param>
        /// <returns></returns>
        private static BaseClientService.Initializer AnalyticsServiceInitializer(string credentialString)
        {
            if (string.IsNullOrEmpty(credentialString))
                return null;

            return new BaseClientService.Initializer
            {
                HttpClientInitializer = GenerateCredential(credentialString),
                ApplicationName = "Google Analytics API Console"
            };
        }


        /// <summary>
        /// Generate Google Credential.
        /// </summary>
        /// <param name="credentialString"></param>
        /// <returns></returns>
        private static ICredential GenerateCredential(string credentialString)
        {
            if (string.IsNullOrEmpty(credentialString))
                return null;

            Byte[] bytes = System.Text.Encoding.ASCII.GetBytes(credentialString);
            using MemoryStream ms = new MemoryStream(bytes);
            return GoogleCredential
                .FromStream(ms)
                .CreateScoped("https://www.googleapis.com/auth/analytics.readonly");
        }
    }    
}