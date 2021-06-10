using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleAnalytics.Library.Common.HealthChecks
{
    public class AppSettingsHealthCheck : IHealthCheck
    {
        /// <summary>
        /// Check the health of the app settings.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<HealthCheckResult> CheckHealthAsync(
              HealthCheckContext context
            , CancellationToken cancellationToken = default)
        {
            if (!ApplicationStatistics.AppSettingsLastLoadedAt.HasValue)
                return Task.FromResult(
                    HealthCheckResult.Unhealthy($"AppSettings have not been loaded! ErrorMessage: '{ApplicationStatistics.AppSettingsLoadErrorMessage}'."));

            if (!ApplicationStatistics.AppSettingsSuccessfullyLoaded)
                return Task.FromResult(
                    HealthCheckResult.Unhealthy($"AppSettings have not been loaded! ErrorMessage: '{ApplicationStatistics.AppSettingsLoadErrorMessage}'."));

            return Task.FromResult(
                HealthCheckResult.Healthy($"AppSettingsService is healthy! Last loaded at '{ApplicationStatistics.AppSettingsLastLoadedAt:yyyy-MM-dd HH.mm.ss.fff}'!"));
        }
    }
}