using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AAG.Global.ExtensionMethods;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using AAG.Global.Health;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace GoogleAnalytics.Library
{
    public class ConsoleAppStartup
    {
        public IConfiguration Configuration { get; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration"></param>
        public ConsoleAppStartup(IConfiguration configuration)
            => Configuration = configuration;


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
            => services.InstallServicesFromAssembly<ConsoleAppStartup>(Configuration);


        /// <summary>
        /// Configuration.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(
              IApplicationBuilder app
            , IWebHostEnvironment env)
        {
            app.UseHealthChecks("/health", new HealthCheckOptions()
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    HealthCheckResponse response = new HealthCheckResponse
                    {
                        Status = report.Status.ToString(),
                        Checks = report.Entries.Select(x => new HealthCheck
                        {
                            Component = x.Key,
                            Status = x.Value.Status.ToString(),
                            Description = x.Value.Description
                        }),
                        Duration = report.TotalDuration
                    };

                    await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
                }
            });
        }
    }
}