using AAG.Global.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AAG.Global.Security;

namespace GoogleAnalytics.Library.Installers
{
    public class SecurityInstaller : IInstaller
    {
        public void InstallServices(
              IServiceCollection services
            , IConfiguration configuration)
        {
            SecurityConfiguration securityConfiguration = new SecurityConfiguration();
            configuration.Bind("SecurityConfiguration", securityConfiguration);
            services.AddSingleton<SecurityConfiguration>(securityConfiguration);
            services.AddSingleton<CryptographyProvider>();
        }
    }
}