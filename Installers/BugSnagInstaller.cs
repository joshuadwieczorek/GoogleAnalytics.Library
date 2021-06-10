using AAG.Global.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Bugsnag.AspNet.Core;

namespace GoogleAnalytics.Library.Installers
{
    public class BugSnagInstaller : IInstaller
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
            services.AddBugsnag(cfg =>
            {
                cfg.ApiKey = configuration["BugsnagApiKey"];
                cfg.AppType = "background-service";
                cfg.AppVersion = configuration["AppReleaseVersion"];
                cfg.ReleaseStage = configuration["AppReleaseStage"];
            });

        }
    }
}