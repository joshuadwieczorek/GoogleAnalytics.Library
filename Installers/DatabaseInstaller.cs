using AAG.Global.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GoogleAnalytics.Library.Data;

namespace GoogleAnalytics.Library.Installers
{
    public class DatabaseInstaller : IInstaller
    {
        public void InstallServices(
              IServiceCollection services
            , IConfiguration configuration)
        {
            services.AddTransient<AccountsDbContext>();
        }
    }
}