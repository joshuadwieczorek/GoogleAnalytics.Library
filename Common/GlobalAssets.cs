using Database.Accounts.Domain.configurations;
using Database.GoogleAnalytics.Domain.queue;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace GoogleAnalytics.Library.Common
{
    public static class GlobalAssets
    {
        private static object threadLock = new object();
        private static List<SrpPagePattern> srpPagePatterns;
        private static ConcurrentQueue<Log> queueLogs = new ConcurrentQueue<Log>();
        
        public static List<SrpPagePattern> SrpPagePatterns 
        {
            get => srpPagePatterns;
            set
            {
                lock (threadLock)
                {
                    if (srpPagePatterns is null)
                        srpPagePatterns = value;
                }
            }
        }


        /// <summary>
        /// Read batch from queue.
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public static Queue<Log> GetQueueLogBatch(int batchSize = 1)
        {
            var i = 0;
            var batch = new Queue<Log>();
            while (!queueLogs.IsEmpty && (i < batchSize))
            {
                if (queueLogs.TryDequeue(out Log queueLog))
                    batch.Enqueue(queueLog);

                ++i;
            }

            return batch;
        }


        /// <summary>
        /// Enqueue queue log.
        /// </summary>
        /// <param name="log"></param>
        public static void Enqueue(Log log)
            => queueLogs.Enqueue(log);
    }
}