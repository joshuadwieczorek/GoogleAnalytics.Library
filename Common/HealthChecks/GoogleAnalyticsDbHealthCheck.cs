using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;
using GoogleAnalytics.Library.Helpers;
using System.Data.SqlClient;
using Newtonsoft.Json;


namespace GoogleAnalytics.Library.Common.HealthChecks
{
    public class GoogleAnalyticsDbHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration"></param>
        public GoogleAnalyticsDbHealthCheck(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString(StaticNames.ConnectionStringGoogleAnalytics);
        }


        /// <summary>
        /// Check health.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<HealthCheckResult> CheckHealthAsync(
              HealthCheckContext context
            , CancellationToken cancellationToken = default)
        {
            try
            {
                var connection = new SqlConnection(_connectionString);
                connection.Open();
                connection.Close();
                return Task.FromResult(
                    HealthCheckResult.Healthy($"GoogleAnalytics database connection suceeded!"));
            }
            catch (Exception e)
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy($"GoogleAnalytics database connection failed! ErrorMessage: '{JsonConvert.SerializeObject(e)}'."));
            }
        }
    }
}