using AAG.Global.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GoogleAnalytics.Library.Services;
using GoogleAnalytics.Library.Common;

namespace GoogleAnalytics.Library.Installers
{
    public class ServicesInstallers : IInstaller
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
            services.AddTransient<ScheduledQueueGeneratorService>();
            services.AddTransient<QueueProcessorService>();
            services.AddTransient<QueueLogProcessorService>();
            services.AddTransient<ManualQueueGeneratorService>();
            services.AddTransient<AppSettingsService>();
        }
    }
}