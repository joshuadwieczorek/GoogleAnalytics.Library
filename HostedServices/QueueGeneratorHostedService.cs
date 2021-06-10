using AAG.Global.Common;
using GoogleAnalytics.Library.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AAG.Global.Enums;
using AAG.Global.ExtensionMethods;
using GoogleAnalytics.Library.Helpers;

namespace GoogleAnalytics.Library.HostedServices
{
    public class QueueGeneratorHostedService : IHostedService
    {
        private readonly ILogger<QueueGeneratorHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private Bugsnag.IClient bugSnag;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="applicationLifetime"></param>
        public QueueGeneratorHostedService(
              ILogger<QueueGeneratorHostedService> logger
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
                var scheduledQueueGeneratorService = scope.ServiceProvider.GetRequiredService<ScheduledQueueGeneratorService>();
                var manualQueueGeneratorService = scope.ServiceProvider.GetRequiredService<ManualQueueGeneratorService>();
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
                        // Add scheduled queue generator service.   
                        if ((processType == ProcessType.Both || processType == ProcessType.Scheduled) 
                            && !tasks.ContainsKey(StaticNames.TaskNameScheduledQueueGenerator) 
                            && AppSettings.ScheduledReportsEnabled)
                        {
                            _logger.LogInformation("Adding scheduled queue generator service!");
                            tasks.Add(StaticNames.TaskNameScheduledQueueGenerator, scheduledQueueGeneratorService.RunAsync(cancellationToken));
                        }

                        // Add manual queue generator service.   
                        if ((processType == ProcessType.Both || processType == ProcessType.Manual) 
                            && !tasks.ContainsKey(StaticNames.TaskNameManualQueueGenerator) 
                            && AppSettings.ManualReportsEnabled)
                        {
                            _logger.LogInformation("Adding manual queue generator service!");
                            tasks.Add(StaticNames.TaskNameManualQueueGenerator, manualQueueGeneratorService.RunAsync(cancellationToken));
                        }
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