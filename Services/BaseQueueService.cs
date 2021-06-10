using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GoogleAnalytics.Library.Data;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using AAG.Global.Security;
using AAG.Global.Common;
using AAG.Global.ExtensionMethods;
using Database.GoogleAnalytics.Domain;
using GoogleAnalytics.Library.Data.Models;
using AAG.Global.Enums;
using System.Data.SqlClient;
using Polly;
using Dapper;
using GoogleAnalytics.Library.Helpers;
using GoogleAnalytics.Library.Data.TableGenerators;
using AAG.Global.Data;

namespace GoogleAnalytics.Library.Services
{
    public abstract class BaseQueueService<T> : BaseActor<T>
    {
        private readonly AccountsDbContext _accountsDbContext;
        private readonly CryptographyProvider _cryptographyProvider;
        protected readonly Polly.Retry.AsyncRetryPolicy retryPolicy;
        private readonly SqlConnection _connection;
        private readonly string _googleConnectionString;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="bugSnag"></param>
        public BaseQueueService(
              ILogger<T> logger
            , Bugsnag.IClient bugSnag
            , IConfiguration configuration
            , AccountsDbContext accountsDbContext
            , CryptographyProvider cryptographyProvider) : base (logger, bugSnag) 
        {
            _accountsDbContext = accountsDbContext;
            _cryptographyProvider = cryptographyProvider;
            _googleConnectionString = configuration.GetConnectionString(StaticNames.ConnectionStringGoogleAnalytics);
            _connection = new SqlConnection(_googleConnectionString);

            retryPolicy = Policy
                .Handle<Exception>(e =>
                {
                    LogError(e);
                    return true;
                })
                .WaitAndRetryAsync(AppSettings.RetryPolicyMaxAttempts, i => TimeSpan.FromMilliseconds(i * AppSettings.RetryPolicyRetryDelayMilliseconds));
        }


        /// <summary>
        /// Queue reports by schedule.
        /// </summary>
        /// <param name="processType"></param>
        /// <param name="reportConfigurations"></param>
        /// <returns></returns>
        public async Task QueueReports(
              ProcessType processType
            , List<Contracts.ReportConfiguration> reportConfigurations)
        {           
            try
            {
                var listGoogleAccts = await _accountsDbContext.ReadGoogleAccounts();
                if (_connection.State != System.Data.ConnectionState.Open)
                    _connection.Open();

                foreach (Contracts.ReportConfiguration report in reportConfigurations)
                {
                    List<QueueItem> listQueues = new List<QueueItem>();
                    AAG.Global.Contracts.DateRange dtRange = new();

                    if ((report.Schedule == Schedule.ManualDaily || report.Schedule == Schedule.ManualMonthly) && report.DateRange.HasValue)
                    {
                        dtRange.StartDate = report.DateRange.Value.StartDate;
                        dtRange.EndDate = report.DateRange.Value.EndDate;
                    }
                    else
                    {
                        dtRange.StartDate = report.Schedule switch
                        {
                            Schedule.ScheduledDaily => DateTime.Today.Yesterday(),
                            Schedule.ScheduledMonthly => DateTime.Today.FirstDayOfLastMonth(),
                            _ => throw new ArgumentException("Invalid schedule for non-manual processing")
                        };

                        dtRange.EndDate = report.Schedule switch
                        {
                            Schedule.ScheduledDaily => DateTime.Today.Yesterday(),
                            Schedule.ScheduledMonthly => DateTime.Today.LastDayOfLastMonth(),
                            _ => throw new ArgumentException("Invalid schedule for non-manual processing")
                        };
                    }
                    
                    var listRanges = new DateRangeCalculator()
                        .Generate(dtRange, report.Schedule);

                    foreach (AAG.Global.Contracts.DateRange dateRange in listRanges)
                    {

                        foreach (var gAcct in listGoogleAccts)
                        {
                            if (!gAcct.Credentials.HasValue())
                                continue;

                            var serializedReport = JsonConvert.SerializeObject(new Data.Models.ReportConfiguration() 
                            { 
                                ReportName = report.Name, 
                                ViewId = gAcct.ViewId, 
                                Filter = report.Filter, 
                                ConnectionString = _googleConnectionString, 
                                DatabaseTableName = report.DatabaseTable, 
                                VdpUrlPatterns = gAcct.VdpUrlPatterns,                                
                                Credentials = gAcct.Credentials, 
                                Dimensions = report.Dimensions, 
                                Metrics = report.Metrics, 
                                ReportDateStart = dateRange.StartDate, 
                                ReportDateEnd = dateRange.EndDate,
                                GoogleId = gAcct.GoogleId
                            });

                            listQueues.Add(new QueueItem() 
                            { 
                                GoogleId = gAcct.GoogleId, 
                                SerializedReport = _cryptographyProvider.Encrypt(serializedReport), 
                                Status = QueueStatus.ReadyToProcess 
                            });
                        }
                    }

                    // Queue reports.
                    await QueueReports(processType, listQueues);
                }

                if (_connection.State != System.Data.ConnectionState.Closed)
                    _connection.Close();
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }


        /// <summary>
        /// Queue reports.
        /// </summary>
        /// <param name="processType"></param>
        /// <param name="queueItems"></param>
        /// <returns></returns>
        private async Task QueueReports(
              ProcessType processType
            , List<QueueItem> queueItems)
        {
            try
            {
                var storedProcedure = processType switch
                {
                    ProcessType.Manual => StaticNames.DbProcQueueManualCreate,
                    _ => StaticNames.DbProcQueueScheduledCreate
                };
                var tableGenerator = new QueueTableTypeGenerator();
                tableGenerator.Populate(queueItems);
                object parameters = new { @QueueTable = new TableValueParameter<QueueItem>(tableGenerator) };
                await _connection.ExecuteAsync(storedProcedure, parameters, commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }
    }
}