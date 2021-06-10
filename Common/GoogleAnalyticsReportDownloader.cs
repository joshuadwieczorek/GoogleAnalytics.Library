using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Google.Apis.AnalyticsReporting.v4.Data;
using Google.Apis.AnalyticsReporting.v4;
using GoogleAnalytics.Library.Data.Models;
using Polly;
using AAG.Global.ExtensionMethods;

namespace GoogleAnalytics.Library.Common
{
    public class GoogleAnalyticsReportDownloader
    {
        private readonly ILogger _logger;
        private readonly Bugsnag.IClient _bugSnag;
        private readonly AnalyticsReportingService _service;
        private readonly Polly.Retry.AsyncRetryPolicy _retryPolicy;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="bugSnag"></param>
        /// <param name="googleAccount"></param>
        public GoogleAnalyticsReportDownloader(
              ILogger logger
            , Bugsnag.IClient bugSnag
            , Database.Accounts.Domain.accounts.Google googleAccount) 
        {           
            _logger = logger;
            _bugSnag = bugSnag;

            _service = AnalyticsReportingServiceManager.AddAndOrFind(googleAccount);           

            int secondsToRetry = 3;

            _retryPolicy = Policy
                .Handle<Exception>(e =>
                {
                    _bugSnag.Notify(e);
                    _logger.LogError("{e}", e);
                    if (e.Message.Contains("Requests per user per 100 seconds"))
                        secondsToRetry = 100;
                    return true;
                })
                .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(1000 * secondsToRetry));
        }


        /// <summary>
        /// Download report.
        /// </summary>
        /// <param name="reportConfiguration"></param>
        /// <returns></returns>
        public async Task<Contracts.ReportResponse> Download(ReportConfiguration reportConfiguration)
        {
            try
            {
                // Ensure report configuration is not null and well.
                if (reportConfiguration is null)
                    throw new ArgumentNullException(nameof(reportConfiguration));

                // Generate report response object.
                var response = GenerateReportResponse(reportConfiguration);

                // Generate google report requests.
                List<ReportRequest> reportRequests = new GoogleReportGenerator(reportConfiguration)
                    .Generate();

                // Get initial report.
                var initialReportResponse = await GetReportsResponse(reportRequests);
                var nextPageToken = initialReportResponse.Reports[0].NextPageToken;

                // Initialize get more reports bool.
                var getMoreReports = initialReportResponse.Reports.Count > 0 
                    && nextPageToken.HasValue();

                // Add initial reports to response.
                response.Reports.AddRange(initialReportResponse.Reports);

                // Loop through and get reports.
                while (getMoreReports)
                {
                    // Wait 5 seconds before running next report.
                    await Task.Delay(5000);

                    // Get report responses.
                    var reportResponse = await GetReportsResponse(reportRequests, nextPageToken);

                    if (reportResponse is not null && reportResponse.Reports.Count > 0)
                    {
                        // If report response is not null add reports to response.
                        response.Reports.AddRange(reportResponse.Reports);

                        // If report response has a next page token, then get the value. 
                        if (reportResponse.Reports[0].NextPageToken.HasValue())
                            nextPageToken = reportResponse.Reports[0].NextPageToken;
                        else
                            // Otherwise, stop looping.
                            getMoreReports = false;
                    }
                }

                // Return report response.
                return response;
            }
            catch (Exception e)
            {
                _bugSnag.Notify(e);
                _logger.LogError("{e}");
            }

            return null;
        }


        /// <summary>
        /// Get google report response.
        /// </summary>
        /// <param name="reportRequests"></param>
        /// <param name="nextPageToken"></param>
        /// <returns></returns>
        private async Task<GetReportsResponse> GetReportsResponse(
              List<ReportRequest> reportRequests
            , string nextPageToken = null)
        {
            // Ensure arguments 
            if (reportRequests is null)
                throw new ArgumentNullException(nameof(reportRequests));

            // Run request in retry policy.
            return await _retryPolicy.ExecuteAsync<GetReportsResponse>(async () =>
            {
                // Set next page token if available.
                if (nextPageToken.HasValue())
                    reportRequests[0].PageToken = nextPageToken;

                // GA report request.
                GetReportsRequest getReportsRequest = new GetReportsRequest { ReportRequests = reportRequests };

                // Initialize batch request.
                ReportsResource.BatchGetRequest batchRequest = _service.Reports.BatchGet(getReportsRequest);

                // Get report request.
                return await batchRequest.ExecuteAsync();
            });
        }


        /// <summary>
        /// Generate a new report response object.
        /// </summary>
        /// <param name="reportConfiguration"></param>
        /// <returns></returns>
        private Contracts.ReportResponse GenerateReportResponse(ReportConfiguration reportConfiguration)
            => new Contracts.ReportResponse
            {
                ViewId = reportConfiguration.ViewId,
                DateRange = new Contracts.DateRange
                {
                    StartDate = reportConfiguration.ReportDateStart,
                    EndDate = reportConfiguration.ReportDateEnd
                },
                Reports = new List<Report>()
            };
    }
}