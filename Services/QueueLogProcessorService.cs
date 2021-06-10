using AAG.Global.Common;
using GoogleAnalytics.Library.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoogleAnalytics.Library.Helpers;
using System.Data.SqlClient;
using Dapper;
using Polly;
using GoogleAnalytics.Library.Data.TableGenerators;
using AAG.Global.Data;
using Database.GoogleAnalytics.Domain.queue;

namespace GoogleAnalytics.Library.Services
{
    public class QueueLogProcessorService : BaseActor<QueueLogProcessorService>
    {        
        private readonly SqlConnection _googleDbConnection;
        protected readonly Polly.Retry.AsyncRetryPolicy retryPolicy;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="bugSnag"></param>
        public QueueLogProcessorService(
              IConfiguration configuration
            , ILogger<QueueLogProcessorService> logger
            , Bugsnag.IClient bugSnag) : base(logger, bugSnag)
        {
            _googleDbConnection = new SqlConnection(configuration.GetConnectionString(StaticNames.ConnectionStringGoogleAnalytics));

            retryPolicy = Policy
                .Handle<Exception>(e =>
                {
                    LogError(e);
                    return true;
                })
                .WaitAndRetryAsync(AppSettings.RetryPolicyMaxAttempts, i => TimeSpan.FromMilliseconds(i * AppSettings.RetryPolicyRetryDelayMilliseconds));
        }


        /// <summary>
        /// Start logging process
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>        
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("QueueLogProcessor starting!");

            int batchSize = AppSettings.QueueLogProcessorBatchSize;
            int waitTimeInSeconds = AppSettings.QueueLogProcessorWaitTimeInSeconds;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var queueLogBatch = GlobalAssets.GetQueueLogBatch(batchSize);
                    if (queueLogBatch.Any())
                    {
                        await retryPolicy.ExecuteAsync(async () =>
                        {
                            using LogTableGenerator logTableGenerator = new LogTableGenerator(StaticNames.DbTypeLogTableType);
                            logTableGenerator.Populate(queueLogBatch);
                            var parameters = new DynamicParameters();
                            parameters.Add("@QueueLogTable", new TableValueParameter<Log>(logTableGenerator));
                            if (_googleDbConnection.State != System.Data.ConnectionState.Open)
                                _googleDbConnection.Open();
                            await _googleDbConnection.ExecuteAsync(StaticNames.DbProcQueueLog, parameters, commandType: System.Data.CommandType.StoredProcedure);
                            if (_googleDbConnection.State != System.Data.ConnectionState.Closed)
                                _googleDbConnection.Close();
                        });
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                }
                await Task.Delay(waitTimeInSeconds * 1000);
            }
        }
    }
}