using AAG.Global.Common;
using GoogleAnalytics.Library.Common.MessageBroker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Database.GoogleAnalytics.Domain.queue;
using GoogleAnalytics.Library.Common;
using GoogleAnalytics.Library.Helpers;

namespace GoogleAnalytics.Library.Services
{
    public class QueueLogMessageBrokerReaderService : BaseActor<QueueLogMessageBrokerReaderService>
    {
        private readonly IMessageBroker _messageBroker;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="bugSnag"></param>
        /// <param name="configuration"></param>
        /// <param name="messageBroker"></param>
        public QueueLogMessageBrokerReaderService(
              ILogger<QueueLogMessageBrokerReaderService> logger
            , Bugsnag.IClient bugSnag
            , IConfiguration configuration
            , IMessageBroker messageBroker) : base(logger, bugSnag)
        {
            _messageBroker = messageBroker;
            _messageBroker.Initialize(configuration[StaticNames.MessageBrokerNameQueueLog]);
        }


        /// <summary>
        /// Read queue processor log messages from queue broker.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("QueueLogMessageBrokerReaderService starting!");

            try
            {
                var queueReaderStarted = false;

                while (!cancellationToken.IsCancellationRequested)
                {
                    // Start queue reader.
                    if (!queueReaderStarted)
                    {
                        _messageBroker.StartQueueReader<Log>(AddMessageToGlobalAssetsLog);
                        queueReaderStarted = true;
                    }

                    await Task.Delay(1000);
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }


        /// <summary>
        /// Add message to global assets.
        /// </summary>
        /// <param name="log"></param>
        private void AddMessageToGlobalAssetsLog(Log log)
            => GlobalAssets.Enqueue(log);
    }
}