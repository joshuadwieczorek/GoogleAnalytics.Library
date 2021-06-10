using Google.Apis.AnalyticsReporting.v4.Data;
using System.Data;
using AAG.Global.Data.Extractors;
using System.Collections.Generic;
using AAG.Global.ExtensionMethods;
using GoogleAnalytics.Library.Contracts;
using Database.Accounts.Domain.configurations;
using GoogleAnalytics.Library.Common;
using System.Linq;

namespace GoogleAnalytics.Library.Utilities
{
    public class AdditionalTableColumnGeneratorUtility
    {
        private readonly ColumnHeader _header;
        private readonly Dictionary<string, List<DataColumnConfiguration>> dimensionColumnExtractors;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="vdpUrlPatterns"></param>
        public AdditionalTableColumnGeneratorUtility(
              ColumnHeader header
            , List<GoogleVdpUrlPattern> vdpUrlPatterns)
        {
            _header = header;
            PageTypeClassifierUtility pageTypeClassifier = new PageTypeClassifierUtility(vdpUrlPatterns, GlobalAssets.SrpPagePatterns);
            IDataExtractor vinExtractor = new VinNumberExtractor();
            dimensionColumnExtractors = new Dictionary<string, List<DataColumnConfiguration>>();
            dimensionColumnExtractors.Add("landingpagepath", new List<DataColumnConfiguration> 
            { 
                new DataColumnConfiguration("landingpagevinnumber", typeof(string), vinExtractor) 
            });
            dimensionColumnExtractors.Add("pagepath", new List<DataColumnConfiguration> 
            { 
                new DataColumnConfiguration("pagepathvinnumber", typeof(string), vinExtractor),
                new DataColumnConfiguration("pagetypeid", typeof(int), pageTypeClassifier: pageTypeClassifier)
            });
            dimensionColumnExtractors.Add("campaign", new List<DataColumnConfiguration> 
            { 
                new DataColumnConfiguration("jobnumber", typeof(string), new JobNumberExtractor()) 
            });
        }


        /// <summary>
        /// Generate additional columns.
        /// </summary>
        /// <returns></returns>
        public List<DataColumn> GenerateAdditionalColumns()
        {
            List<DataColumn> columns = new List<DataColumn>();

            if (_header?.Dimensions is null || !_header.Dimensions.Any())
                return columns;

            foreach (var dimension in _header.Dimensions)
                if (dimensionColumnExtractors.TryGetValue(dimension.Lower().Replace("ga:", string.Empty), out List<DataColumnConfiguration> configs))
                    foreach (var config in configs)
                        columns.Add(new DataColumn(config.ColumnName, config.DataType));

            return columns;
        }


        /// <summary>
        /// Add custom row data.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="dimensionName"></param>
        /// <param name="dimensionValue"></param>
        public void AddCustomRowColumn(
              ref DataRow row
            , string dimensionName
            , string dimensionValue)
        {
            if (_header?.Dimensions is null || !_header.Dimensions.Any())
                return;

            if (dimensionColumnExtractors.TryGetValue(dimensionName.Lower(), out List<DataColumnConfiguration> configs))
            {
                foreach (var config in configs)
                {
                    if (config.DataExtractor is not null)
                        row[config.ColumnName] = config.DataExtractor.Extract(dimensionValue);

                    else if (config.PageTypeClassifier is not null)
                        row[config.ColumnName] = config.PageTypeClassifier.Classify(dimensionValue);
                }
            }
        }
    }
}