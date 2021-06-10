using AAG.Global.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleAnalytics.Library.Installers
{
    public class HealthChecksInstaller : IInstaller
    {
        /// <summary>
        /// Install main services.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public void InstallServices(
              IServiceCollection services
            , IConfiguration configuration)
        {
            services.AddHealthChecks();
        }
    }
}