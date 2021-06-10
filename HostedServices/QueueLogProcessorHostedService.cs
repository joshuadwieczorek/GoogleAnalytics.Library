using AAG.Global.Common;
using GoogleAnalytics.Library.Helpers;
using GoogleAnalytics.Library.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleAnalytics.Library.HostedServices
{
    public class QueueLogProcessorHostedService : IHostedService
    {
        private readonly ILogger<QueueLogProcessorHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private Bugsnag.IClient bugSnag;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="applicationLifetime"></param>
        public QueueLogProcessorHostedService(
              ILogger<QueueLogProcessorHostedService> logger
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
                var queueLogMessageBrokerReaderService = scope.ServiceProvider.GetRequiredService<QueueLogMessageBrokerReaderService>();
                var queueLogProcessor = scope.ServiceProvider.GetRequiredService<QueueLogProcessorService>();                
                bugSnag = scope.ServiceProvider.GetRequiredService<Bugsnag.IClient>();
                var tasks = new Dictionary<string, Task>();

                try
                {
                    // Start app services.                    

                    // Add app settings service.
                    _logger.LogInformation("Adding app settings service!");
                    tasks.Add(StaticNames.TaskNameAppSettings, appSettingsService.RunAsync(cancellationToken));

                    // While cancellation token is not canceled.
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // Add message queue log processor service.   
                        if (!tasks.ContainsKey(StaticNames.TaskNameQueueLogMessageBrokerReader))
                        {
                            _logger.LogInformation("Adding queue log message broker service!");
                            tasks.Add(StaticNames.TaskNameQueueLogMessageBrokerReader, queueLogMessageBrokerReaderService.RunAsync(cancellationToken));
                        }

                        // Add queue log processor service.   
                        if (!tasks.ContainsKey(StaticNames.TaskNameQueueLogProcessor))
                        {
                            _logger.LogInformation("Adding queue log processor service!");
                            tasks.Add(StaticNames.TaskNameQueueLogProcessor, queueLogProcessor.RunAsync(cancellationToken));
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