using System;
using AAG.Global.Data.Extractors;
using GoogleAnalytics.Library.Utilities;

namespace GoogleAnalytics.Library.Contracts
{
    public struct DataColumnConfiguration
    {
        public string ColumnName { get; init; }
        public Type DataType { get; set; }
        public IDataExtractor DataExtractor { get; init; }
        public PageTypeClassifierUtility PageTypeClassifier { get; set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="dataExtractor"></param>
        public DataColumnConfiguration(
              string columnName
            , Type dataType
            , IDataExtractor dataExtractor = null
            , PageTypeClassifierUtility pageTypeClassifier = null)
        {
            ColumnName = columnName;
            DataType = dataType;
            DataExtractor = dataExtractor;
            PageTypeClassifier = pageTypeClassifier;
        }
    }
}