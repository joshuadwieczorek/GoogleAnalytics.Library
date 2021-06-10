using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using GoogleAnalytics.Library.Data;
using GoogleAnalytics.Library.Contracts;
using System.IO;
using Newtonsoft.Json;
using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AAG.Global.ExtensionMethods;
using AAG.Global.Security;
using GoogleAnalytics.Library.Helpers;
using AAG.Global.Enums;

namespace GoogleAnalytics.Library.Services
{
    public class ScheduledQueueGeneratorService :  BaseQueueService<ScheduledQueueGeneratorService>
    {
        private DateTime nextRunDate;
        private DateTime processStartTime;
        private string pickupFolderPath;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="bugSnag"></param>
        /// <param name="configuration"></param>
        /// <param name="accountsDbContext"></param>
        /// <param name="cryptographyProvider"></param>
        public ScheduledQueueGeneratorService(
              ILogger<ScheduledQueueGeneratorService> logger
            , Bugsnag.IClient bugSnag
            , IConfiguration configuration
            , AccountsDbContext accountsDbContext
            , CryptographyProvider cryptographyProvider) : base(logger, bugSnag, configuration, accountsDbContext, cryptographyProvider)
        {
            nextRunDate = DateTime.Now.AddMinutes(-1);
        }


        /// <summary>
        /// On application startup.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("ScheduledQueueGeneratorService starting!");

            while (!cancellationToken.IsCancellationRequested)
            {
                if (DateTime.Now >= nextRunDate)
                {
                    // Set process start date time.
                    processStartTime = DateTime.Now;

                    try
                    {
                        // Set pickup path.
                        pickupFolderPath = AppSettings.PathScheduledReportsPickup;

                        // Validate folder paths.
                        ValidateFolderPaths();

                        // Get files.
                        var files = Directory.GetFiles(pickupFolderPath, $"*{AppSettings.FileExtensionScheduledReportFiles}");

                        // Process each file.
                        foreach (var file in files)
                        {
                            // Read report file.
                            var jsonFile = await retryPolicy.ExecuteAsync<string>(() => File.ReadAllTextAsync(file));

                            // Convert to reports.
                            var listReports = JsonConvert.DeserializeObject<List<ReportConfiguration>>(jsonFile);

                            // Queue all monthly reports.
                            if (DateTime.Now.Day == 1)
                            {
                                var monthlyReports = listReports
                                    .Where(r => r.Schedule == AAG.Global.Enums.Schedule.ScheduledMonthly)
                                    .ToList();

                                await QueueReports(ProcessType.Scheduled, monthlyReports);
                            }

                            // Queue all daily reports.
                            var dailyReports = listReports
                                .Where(r => r.Schedule == AAG.Global.Enums.Schedule.ScheduledDaily)
                                .ToList();

                            await QueueReports(ProcessType.Scheduled, dailyReports);
                        }
                    }
                    catch (Exception e)
                    {
                        LogError(e);
                    }

                    // Set next run date.
                    SetNextRunDate();
                }
                else
                    await Task.Delay(1000);
            }
        }


        /// <summary>
        /// Set next run date.
        /// </summary>
        private void SetNextRunDate()
        {
            nextRunDate = processStartTime
                .AddMilliseconds(AppSettings.QueueGeneratorScheduledMilliseconds)
                .AddSeconds(AppSettings.QueueGeneratorScheduledSeconds)
                .AddMinutes(AppSettings.QueueGeneratorScheduledMinutes)
                .AddHours(AppSettings.QueueGeneratorScheduledHours)
                .AddDays(AppSettings.QueueGeneratorScheduledDays);
        }


        /// <summary>
        /// Validate folder paths.
        /// </summary>
        private void ValidateFolderPaths()
        {
            if (!pickupFolderPath.IsDirectory())
                throw new DirectoryNotFoundException(pickupFolderPath);
        }
    }
}