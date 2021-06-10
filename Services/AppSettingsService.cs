using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Database.GoogleAnalytics.Domain.dbo;
using GoogleAnalytics.Library.Helpers;
using GoogleAnalytics.Library.Common;
using System.Threading;
using AAG.Global.ExtensionMethods;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using GoogleAnalytics.Library.Helpers.Urls;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleAnalytics.Library.Services
{
    public class AppSettingsService
    {
        private readonly ILogger<AppSettingsService> _logger;
        private readonly int _appSettingsServiceDelayInMinutes;
        private readonly Bugsnag.IClient _bugSnag;
        private readonly string _appSettingsUrl;
        private DateTime nextSyncDate;      


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="bugSnag"></param>
        /// <param name="configuration"></param>
        public AppSettingsService(
              ILogger<AppSettingsService> logger
            , IConfiguration configuration
            , Bugsnag.IClient bugSnag) 
        {
            _logger = logger;
            _bugSnag = bugSnag;
            _appSettingsServiceDelayInMinutes = configuration[StaticNames.ConfigAppSettingsServiceDelayInMinutes].ToInt();            
            _appSettingsUrl = UrlGenerator.Generate(configuration[StaticNames.ConfigAppSettingsUrl], LocalEndpoints.AppSettings);
            nextSyncDate = DateTime.Now.AddMinutes(-1);
        }


        /// <summary>
        /// Run the task async.
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AppSettingsService starting!");

            while (!cancellationToken.IsCancellationRequested)
            {
                if (DateTime.Now >= nextSyncDate)
                {
                    _logger.LogInformation("AppSettings starting to load!");
                    try
                    {
                        using var client = new HttpClient();
                        var response = await client.GetAsync(_appSettingsUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var now = DateTime.Now;
                            var jsonString = await response.Content.ReadAsStringAsync();
                            var configurations = JsonConvert.DeserializeObject<List<Configuration>>(jsonString);
                            AppSettings.Set(configurations);
                            ApplicationStatistics.AppSettingsLastLoadedAt = now;
                            ApplicationStatistics.AppSettingsSuccessfullyLoaded = true;
                            nextSyncDate = now.AddMinutes(_appSettingsServiceDelayInMinutes);
                            _logger.LogInformation("AppSettings loaded!");
                        }
                        else
                        {
                            var responseMessage = await response.Content.ReadAsStringAsync();
                            _logger.LogError("AppSettings failed to load!");
                            _logger.LogError($"{responseMessage}");
                            ApplicationStatistics.AppSettingsSuccessfullyLoaded = false;
                            ApplicationStatistics.AppSettingsLoadErrorMessage = responseMessage;
                            nextSyncDate = DateTime.Now.AddMinutes(1);
                        }

                    }
                    catch (Exception e)
                    {
                        ApplicationStatistics.AppSettingsSuccessfullyLoaded = false;
                        ApplicationStatistics.AppSettingsLoadErrorMessage = e.Message;
                        _bugSnag.Notify(e);
                        _logger.LogError("{e}", e);
                    }
                }
                else
                    await Task.Delay(1000);
            }
        }
    }
}