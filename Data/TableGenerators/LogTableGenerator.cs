using AAG.Global.Data;
using Database.GoogleAnalytics.Domain.queue;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace GoogleAnalytics.Library.Data.TableGenerators
{
    public class LogTableGenerator : TableGeneratorBase<Log>
    {
        public LogTableGenerator(string tableName) : base(tableName) { }

        public override void Populate(IEnumerable<Log> rows)
        {
            if (rows is null || !rows.Any())
                return;

            foreach (var row in rows)
                Populate(row);
        }

        protected override void Construct()
        {
            Table.Columns.Add(new DataColumn("QueueId", typeof(long)));
            Table.Columns.Add(new DataColumn("GoogleId", typeof(int)));
            Table.Columns.Add(new DataColumn("BatchId", typeof(string)));
            Table.Columns.Add(new DataColumn("QueueStatus", typeof(int)));
            Table.Columns.Add(new DataColumn("ProcessType", typeof(int)));
            Table.Columns.Add(new DataColumn("Message", typeof(string)));
            Table.Columns.Add(new DataColumn("CreatedBy", typeof(string)));
            Table.Columns.Add(new DataColumn("CreatedAt", typeof(DateTime)));
        }

        protected override void Populate(Log log)
        {
            DataRow row = Table.NewRow();
            row["QueueId"] = log.QueueId;
            row["GoogleId"] = log.GoogleId;
            row["BatchId"] = log.BatchId;
            row["QueueStatus"] = log.QueueStatus;
            row["ProcessType"] = log.ProcessType;
            row["Message"] = log.Message;
            row["CreatedBy"] = log.CreatedBy;
            row["CreatedAt"] = log.CreatedAt;

            Table.Rows.Add(row);
        }
    }
}
