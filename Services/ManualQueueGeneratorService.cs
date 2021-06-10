using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GoogleAnalytics.Library.Data;
using GoogleAnalytics.Library.Contracts;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AAG.Global.ExtensionMethods;
using AAG.Global.Security;
using GoogleAnalytics.Library.Helpers;
using AAG.Global.Enums;

namespace GoogleAnalytics.Library.Services
{
    public class ManualQueueGeneratorService : BaseQueueService<ManualQueueGeneratorService>
    {
        private string pickupFolderPath;
        private string archivedFolderPath;
        private string failedFolderPath;
        private DateTime nextRunDate;
        private DateTime processStartTime;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="bugSnag"></param>
        /// <param name="configuration"></param>
        /// <param name="accountsDbContext"></param>
        /// <param name="cryptographyProvider"></param>
        public ManualQueueGeneratorService(
              ILogger<ManualQueueGeneratorService> logger
            , Bugsnag.IClient bugSnag
            , IConfiguration configuration
            , AccountsDbContext accountsDbContext
            , CryptographyProvider cryptographyProvider) : base(logger, bugSnag, configuration, accountsDbContext, cryptographyProvider)
        {            
            nextRunDate = DateTime.Now.AddMinutes(-1);
        }


        /// <summary>
        /// Queue manual reports.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("ManualQueueGeneratorService starting!");

            while (!cancellationToken.IsCancellationRequested)
            {
                if (DateTime.Now >= nextRunDate)
                {
                    // Set process start date time.
                    processStartTime = DateTime.Now;

                    try
                    {
                        // Set folder paths.
                        pickupFolderPath = AppSettings.PathManualReportsPickup;
                        archivedFolderPath = AppSettings.PathManualReportsArchive;
                        failedFolderPath = AppSettings.PathManualReportsFailed;

                        // Validate folder paths.
                        ValidateFolderPaths();

                        // Get files to process.
                        var files = Directory.GetFiles(pickupFolderPath, $"*{AppSettings.FileExtensionManualReportFiles}");

                        foreach (var file in files)
                        {
                            // Bail if need to exit app.
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            var fileInfo = new FileInfo(file);

                            try
                            {
                                var jsonFile = await retryPolicy.ExecuteAsync<string>(() => File.ReadAllTextAsync(file));
                                var listReports = JsonConvert.DeserializeObject<List<ReportConfiguration>>(jsonFile);
                                ValidateFileReports(file, listReports);
                                await QueueReports(ProcessType.Manual, listReports);
                                var archivedFilePath = Path.Combine(archivedFolderPath, $"{fileInfo.Name.Replace(fileInfo.Extension, string.Empty)}_processedAt_{DateTime.Now:yyyy.MM.dd_HH.mm.ss.fffffff}{fileInfo.Extension}");
                                File.Move(Path.GetFullPath(file), archivedFilePath);
                            }
                            catch (Exception e)
                            {
                                LogError(e);
                                var failedFilePath = Path.Combine(failedFolderPath, $"{fileInfo.Name.Replace(fileInfo.Extension, string.Empty)}_failedAt_{DateTime.Now:yyyy.MM.dd_HH.mm.ss.fffffff}{fileInfo.Extension}");
                                File.Move(Path.GetFullPath(file), failedFilePath);
                            }
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
                .AddMilliseconds(AppSettings.QueueGeneratorManualMilliseconds)
                .AddSeconds(AppSettings.QueueGeneratorManualSeconds)
                .AddMinutes(AppSettings.QueueGeneratorManualMinutes)
                .AddHours(AppSettings.QueueGeneratorManualHours)
                .AddDays(AppSettings.QueueGeneratorManualDays);
        }


        /// <summary>
        /// Validate folder paths.
        /// </summary>
        private void ValidateFolderPaths()
        {
            if (!pickupFolderPath.IsDirectory())
                throw new DirectoryNotFoundException(pickupFolderPath);

            if (!archivedFolderPath.IsDirectory())
                throw new DirectoryNotFoundException(archivedFolderPath);

            if (!archivedFolderPath.IsDirectory())
                throw new DirectoryNotFoundException(failedFolderPath);
        }


        /// <summary>
        /// Validate file reports.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="reportConfigurations"></param>
        private void ValidateFileReports(
              string filePath
            , List<ReportConfiguration> reportConfigurations)
        {
            if (reportConfigurations.Exists(r => !r.DateRange.HasValue))
            {
                var exception = new ArgumentNullException($"Date range(s) are null for file '{filePath}'!");
                exception.Data.Add("FilePath", filePath);
                exception.Data.Add("Reports", JsonConvert.SerializeObject(reportConfigurations));
                throw exception;
            }
        }
    }
}