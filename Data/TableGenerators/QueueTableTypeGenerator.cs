using AAG.Global.Data;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using GoogleAnalytics.Library.Data.Models;

namespace GoogleAnalytics.Library.Data.TableGenerators
{
    internal class QueueTableTypeGenerator : TableGeneratorBase<QueueItem>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        internal QueueTableTypeGenerator() : base("[queue].[QueueTableType]") { }


        /// <summary>
        /// Populate table from rows.
        /// </summary>
        /// <param name="rows"></param>
        public override void Populate(IEnumerable<QueueItem> rows)
        {
            if (rows is null || !rows.Any())
                return;

            foreach (var row in rows)
                Populate(row);
        }


        /// <summary>
        /// Construct data table.
        /// </summary>
        protected override void Construct()
        {
            Table.Columns.Add(new DataColumn("GoogleId", typeof(int)));
            Table.Columns.Add(new DataColumn("SerializedReport", typeof(string)));
            Table.Columns.Add(new DataColumn("Status", typeof(int)));
        }


        /// <summary>
        /// Populate table row.
        /// </summary>
        /// <param name="row"></param>
        protected override void Populate(QueueItem row)
        {
            var tableRow = Table.NewRow();
            tableRow["GoogleId"] = row.GoogleId;
            tableRow["SerializedReport"] = row.SerializedReport;
            tableRow["Status"] = row.Status;
            Table.Rows.Add(tableRow);
        }
    }
}