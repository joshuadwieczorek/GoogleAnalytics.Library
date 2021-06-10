using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AAG.Global.ExtensionMethods;
using Google.Apis.AnalyticsReporting.v4.Data;

namespace GoogleAnalytics.Library.Common
{
    public class DataTableGenerator : IDisposable
    {
         public DataTable Table { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseTableName"></param>
        /// <param name="header"></param>
        /// <param name="addtnlColumns"></param>
        public DataTableGenerator(string databaseTableName, ColumnHeader header, List<DataColumn> addtnlColumns = null)
        {
            Construct(databaseTableName, header, addtnlColumns);
        }


        /// <summary>
        /// Generate data table.
        /// </summary>
        /// <param name="databaseTableName"></param>
        /// <param name="header"></param>
        /// <param name="addtnlColumns"></param>
        private void Construct(
              string databaseTableName
            , ColumnHeader header
            , List<DataColumn> addtnlColumns = null)
        {
            Table = new DataTable(databaseTableName);
            Table.Columns.Add(new DataColumn("id", typeof(long)));
            Table.Columns.Add(new DataColumn("googleid", typeof(int)));
            Table.Columns.Add(new DataColumn("reportstartdate", typeof(DateTime)));
            Table.Columns.Add(new DataColumn("reportenddate", typeof(DateTime)));
            Table.Columns.Add(new DataColumn("createdat", typeof(DateTime)));
            Table.Columns.Add(new DataColumn("createdby", typeof(string)));

            if (header.Dimensions is not null && header.Dimensions.Any())
                foreach (String dimension in header.Dimensions)
                    Table.Columns.Add(new DataColumn(dimension.ToLower().Replace("ga:", ""), typeof(string)));

            if (header.MetricHeader?.MetricHeaderEntries is not null && header.MetricHeader.MetricHeaderEntries.Any())
                foreach (MetricHeaderEntry metric in header.MetricHeader.MetricHeaderEntries)
                    Table.Columns.Add(new DataColumn(metric.Name.Lower(), GetType(metric.Type.Upper())));

            if (addtnlColumns is not null && addtnlColumns.Any())
                Table.Columns.AddRange(addtnlColumns.ToArray());            
        }
        
        /// <summary>
        /// Get type by string.
        /// </summary>
        /// <param name="typeString"></param>
        /// <returns></returns>
        protected Type GetType(string typeString)
        {
            if (!typeString.HasValue())
                return typeof(string);

            Type type = null;

            switch (typeString.ToUpper())
            {
                case "INTEGER":
                    type = typeof(long);
                    break;
                case "PERCENT":
                case "FLOAT":
                case "CURRENCY":
                case "TIME":
                    type = typeof(decimal);
                    break;
            }
            return type;
        }


        /// <summary>
        /// Garbage cleanup
        /// </summary>
        public void Dispose()
        {
            Table?.Dispose();
        }
    }
}