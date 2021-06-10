using AAG.Global.Common;
using AAG.Global.Enums;
using AAG.Global.ExtensionMethods;
using GoogleAnalytics.Library.Helpers;
using GoogleAnalytics.Library.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleAnalytics.Library.HostedServices
{
    public class QueueProcessorHostedService : IHostedService
    {
        private readonly ILogger<QueueProcessorHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private Bugsnag.IClient bugSnag;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="applicationLifetime"></param>
        public QueueProcessorHostedService(
              ILogger<QueueProcessorHostedService> logger
            , IServiceProvider serviceProvider
            , IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _applicationLifetime = applicationLifetime;
        }


        /// <summary>
        /// Start the app.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Application starting!");
                var scope = _serviceProvider.CreateScope();
                var appSettingsService = scope.ServiceProvider.GetRequiredService<AppSettingsService>();
                var arguments = scope.ServiceProvider.GetRequiredService<ApplicationArguments>();
                var secheduledQueueProcessorService = scope.ServiceProvider.GetRequiredService<QueueProcessorService>();
                var manualQueueProcessorService = scope.ServiceProvider.GetRequiredService<QueueProcessorService>();
                bugSnag = scope.ServiceProvider.GetRequiredService<Bugsnag.IClient>();
                var tasks = new Dictionary<string, Task>();

                try
                {
                    // Get process type.
                    var processType = arguments.Arguments.Any()
                        ? arguments.Arguments.First().ToEnum<ProcessType>(ProcessType.None)
                        : ProcessType.Both;

                    // If invalid process type was passed, bail.
                    if (processType == ProcessType.None)
                    {
                        _logger.LogWarning("Invalid process type argument!");
                        _applicationLifetime.StopApplication();
                    }

                    // Start app services.                    

                    // Add app settings service.
                    _logger.LogInformation("Adding app settings service!");
                    tasks.Add(StaticNames.TaskNameAppSettings, appSettingsService.RunAsync(cancellationToken));

                    // While cancellation token is not canceled.
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // Add scheduled report processor service.   
                        if ((processType == ProcessType.Both || processType == ProcessType.Scheduled)
                            && !tasks.ContainsKey(StaticNames.TaskNameProcessScheduledReports)
                            && AppSettings.ScheduledReportsEnabled)
                        {
                            _logger.LogInformation("Adding scheduled report processor service!");
                            tasks.Add(StaticNames.TaskNameProcessScheduledReports, secheduledQueueProcessorService.RunAsync(cancellationToken, ProcessType.Scheduled));
                        }

                        // Add manual report processor service.   
                        if ((processType == ProcessType.Both || processType == ProcessType.Manual)
                            && !tasks.ContainsKey(StaticNames.TaskNameProcessManualReports)
                            && AppSettings.ManualReportsEnabled)
                        {
                            _logger.LogInformation("Adding manual report processor service!");
                            tasks.Add(StaticNames.TaskNameProcessManualReports, secheduledQueueProcessorService.RunAsync(cancellationToken, ProcessType.Manual));
                        }

                        await Task.Delay(1000);
                    }

                    // Stop application when all tasks complete.
                    _applicationLifetime.StopApplication();
                    await Task.CompletedTask;
                }
                catch (Exception e)
                {
                    bugSnag.Notify(e);
                    _logger.LogError("{e}", e);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("{e}", e);
            }
        }


        /// <summary>
        /// On application stopping.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Application stopping!");
            await Task.CompletedTask;
        }
    }
}