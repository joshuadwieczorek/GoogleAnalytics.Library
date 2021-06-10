using AAG.Global.ExtensionMethods;
using AAG.Global.Security;
using Google.Apis.AnalyticsReporting.v4.Data;
using GoogleAnalytics.Library.Common;
using GoogleAnalytics.Library.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Database.GoogleAnalytics.Domain;
using GoogleAnalytics.Library.Utilities;
using AAG.Global.Enums;
using System.Collections.Generic;
using System.Data.SqlClient;
using Dapper;
using GoogleAnalytics.Library.Helpers;
using System.Collections.Concurrent;
using AAG.Global.Common;
using GoogleAnalytics.Library.Common.MessageBroker;

namespace GoogleAnalytics.Library.Services
{
    public class QueueProcessorService : BaseActor<QueueProcessorService>
    {
        private readonly object _threadLock;
        private readonly CryptographyProvider _cryptography;
        private readonly IMessageBroker _messageBroker;
        private readonly ConcurrentDictionary<string, Task> _batches;
        private readonly SqlConnection _googleDbConnection;
        

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="cryptography"></param>
        /// <param name="bugSnag"></param>
        public QueueProcessorService(
              ILogger<QueueProcessorService> logger
            , IConfiguration configuration
            , CryptographyProvider cryptography
            , Bugsnag.IClient bugSnag
            , IMessageBroker messageBroker) : base(logger, bugSnag)
        {
            _threadLock = new object();
            _cryptography = cryptography;
            _messageBroker = messageBroker;
            _messageBroker.Initialize(configuration[StaticNames.MessageBrokerNameQueueLog]);
            _batches = new ConcurrentDictionary<string, Task>();
            _googleDbConnection = new SqlConnection(configuration.GetConnectionString(StaticNames.ConnectionStringGoogleAnalytics));
        }


        /// <summary>
        /// Run the queue generator.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RunAsync(
              CancellationToken cancellationToken
            , ProcessType processType)
        {
            try
            {
                logger.LogInformation("QueueProcessor starting!");

                // Bail if app stopping requested. 
                if (cancellationToken.IsCancellationRequested)
                    await Task.CompletedTask;

                // Get batch size.
                var batchSize = processType switch
                {
                    ProcessType.Manual => AppSettings.ManualReportsSimultaneousBatches,
                    _ => AppSettings.ScheduledReportsSimultaneousBatches
                };

                // Create batches.
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_batches.Count >= batchSize)
                        continue;

                    var guid = Guid.NewGuid().ToString();
                    _batches.TryAdd(guid, ProcessBatch(cancellationToken, processType, guid));
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }


        /// <summary>
        /// Process queue batch.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="processType"></param>
        /// <param name="batchId"></param>
        /// <returns></returns>
        private async Task ProcessBatch(
              CancellationToken cancellationToken
            , ProcessType processType
            , string batchId)
        {
            try
            {
                // Bail if app stopping requested. 
                if (cancellationToken.IsCancellationRequested)
                    await Task.CompletedTask;

                // Get dequeued reports.
                var dequeuedReports = DequeueReports(processType);

                // Loop through and process each report.
                foreach (var dequeuedReport in dequeuedReports)
                {
                    // Bail if app stopping requested.
                    if (cancellationToken.IsCancellationRequested)
                        await Task.CompletedTask;

                    // Process report.
                    await ProcessDequeuedReport(cancellationToken, batchId, processType, dequeuedReport);
                }                

                // Wait a time before competing task.
                var waitTimeInSeconds = processType switch
                {
                    ProcessType.Manual => AppSettings.ManualReportsWaitTimeInSeconds,
                    _ => AppSettings.ScheduledReportsWaitTimeInSeconds
                };
                await Task.Delay(1000 * waitTimeInSeconds);

                // Remove from batch dictionary.
                _batches.TryRemove(batchId, out Task completedTask);
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }


        /// <summary>
        /// Process dequeued report.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="batchId"></param>
        /// <param name="processType"></param>
        /// <param name="queueItem"></param>
        /// <returns></returns>
        private async Task ProcessDequeuedReport(
              CancellationToken cancellationToken
            , string batchId
            , ProcessType processType
            , QueueItem queueItem)
        {
            var message = string.Empty;

            if (cancellationToken.IsCancellationRequested)
                await Task.CompletedTask;

            try
            {
                var reportConfiguration = JsonConvert.DeserializeObject<ReportConfiguration>(_cryptography.Decrypt(queueItem.SerializedReport));

                Database.Accounts.Domain.accounts.Google googleAccount = new()
                {
                    ViewId = reportConfiguration.ViewId,
                    Credentials = _cryptography.Decrypt(reportConfiguration.Credentials)
                };

                var downloader = new GoogleAnalyticsReportDownloader(logger, bugSnag, googleAccount);
                var response = await downloader.Download(reportConfiguration);

                if (response?.Reports is null || !response.Reports.Any())
                    throw new ArgumentNullException("reports are empty");

                await ProcessResponse(reportConfiguration, response);
                queueItem.Status = QueueStatus.Processed;
            }
            catch (Exception e)
            {
                message = e.Message;
                queueItem.Status = QueueStatus.Failed;
                LogError(e);
            }

            // Update queue status.
            UpdateQueueStatus(processType, queueItem.QueueId, queueItem.Status);

            // Create and queue log entry.
            QueueLogEntry(processType, batchId, message, queueItem);
        }


        /// <summary>
        /// Process queue report.
        /// </summary>
        /// <param name="reportConfiguration"></param>
        /// <param name="response"></param>
        private async Task ProcessResponse(
              ReportConfiguration reportConfiguration
            , Contracts.ReportResponse response)
        {

            if (reportConfiguration is null || response?.Reports is null)
                throw new ArgumentNullException("reportConfiguration or response?.reports is null");

            var reportHeader = response.Reports.First()?.ColumnHeader;

            var additionalColumnUtility = new AdditionalTableColumnGeneratorUtility(reportHeader, reportConfiguration.VdpUrlPatterns);
            var additionalColumns = additionalColumnUtility.GenerateAdditionalColumns();
            using var tableGenerator = new DataTableGenerator(reportConfiguration.DatabaseTableName, reportHeader, additionalColumns);

            foreach (Report report in response.Reports)
            {
                if (report.Data?.Rows is null)
                    continue;

                foreach (ReportRow row in report.Data.Rows)
                {
                    if (row is null)
                        continue;

                    DataRow dataRow = tableGenerator.Table.NewRow();

                    dataRow["id"] = 0;
                    dataRow["googleid"] = reportConfiguration.GoogleId;
                    dataRow["reportstartdate"] = reportConfiguration.ReportDateStart;
                    dataRow["reportenddate"] = reportConfiguration.ReportDateEnd;
                    dataRow["createdat"] = DateTime.Now;
                    dataRow["createdby"] = Environment.UserName;

                    for (int i = 0; i < reportHeader.Dimensions.Count; i++)
                    {
                        var dimensionName = reportHeader.Dimensions[i].Replace("ga:", string.Empty).Lower();
                        dataRow[dimensionName] = row.Dimensions[i];
                        additionalColumnUtility.AddCustomRowColumn(ref dataRow, dimensionName, row.Dimensions[i]);                      
                    }

                    for(int i = 0; i < reportHeader.MetricHeader.MetricHeaderEntries.Count; i++)
                    {
                        var metricName = reportHeader.MetricHeader.MetricHeaderEntries[i].Name.Lower();
                        dataRow[metricName] = row.Metrics[0].Values[i];
                    }

                    tableGenerator.Table.Rows.Add(dataRow);
                }
            }

            // Pump data into the database.
            using var connection = new SqlConnection(reportConfiguration.ConnectionString);
            connection.Open();
            using SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);
            foreach (DataColumn column in tableGenerator.Table.Columns)
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            bulkCopy.DestinationTableName = tableGenerator.Table.TableName;
            await bulkCopy.WriteToServerAsync(tableGenerator.Table);
            connection.Close();
        }


        /// <summary>
        /// Queue log entry for dabase entry.
        /// </summary>
        /// <param name="processType"></param>
        /// <param name="batchId"></param>
        /// <param name="message"></param>
        /// <param name="queueItem"></param>
        private void QueueLogEntry(
              ProcessType processType
            , string batchId
            , string message
            , QueueItem queueItem)
        {
            lock (_threadLock)
            {
                Database.GoogleAnalytics.Domain.queue.Log log = new()
                {
                    QueueId = queueItem.QueueId,
                    GoogleId = queueItem.GoogleId,
                    Message = message,
                    CreatedAt = DateTime.Now,
                    CreatedBy = Environment.UserName,
                    ProcessType = processType,
                    QueueStatus = queueItem.Status,
                    BatchId = batchId
                };
                _messageBroker.PublishMessage<Database.GoogleAnalytics.Domain.queue.Log>(log, true);
            }
        }


        /// <summary>
        /// Dequeue reports to process.
        /// </summary>
        /// <param name="processType"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        private List<QueueItem> DequeueReports(ProcessType processType)
        {
            lock (_threadLock)
            {
                var storedProcedure = processType switch
                {
                    ProcessType.Manual => StaticNames.DbProcQueueManualRead,
                    _ => StaticNames.DbProcQueueScheduledRead
                };
                var batchSize = processType switch
                {
                    ProcessType.Manual => AppSettings.ManualReportsQueueBatchSize,
                    _ => AppSettings.ScheduledReportsQueueBatchSize
                };
                var parameters = new DynamicParameters();
                parameters.Add("@BatchSize", batchSize);
                _googleDbConnection.Open();
                var deQueuedReports = _googleDbConnection.Query<QueueItem>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
                _googleDbConnection.Close();
                return deQueuedReports.ToList();
            }
        }


        /// <summary>
        /// Update queue item.
        /// </summary>
        /// <param name="processType"></param>
        /// <param name="queueId"></param>
        /// <param name="status"></param>
        private void UpdateQueueStatus(
              ProcessType processType
            , long queueId
            , QueueStatus status)
        {
            lock (_threadLock)
            {
                var storedProcedure = processType switch
                {
                    ProcessType.Manual => StaticNames.DbProcQueueManualUpdate,
                    _ => StaticNames.DbProcQueueScheduledUpdate
                };
                var parameters = new DynamicParameters();
                parameters.Add("@QueueId", queueId);
                parameters.Add("@QueueStatus", status);
                _googleDbConnection.Open();
                _googleDbConnection.Execute(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
                _googleDbConnection.Close();
            }
        }
    }
}