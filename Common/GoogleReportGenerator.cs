using AAG.Global.ExtensionMethods;
using Google.Apis.AnalyticsReporting.v4.Data;
using GoogleAnalytics.Library.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleAnalytics.Library.Common
{
    internal sealed class GoogleReportGenerator
    {
        private readonly ReportConfiguration _reportConfiguration;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="reportConfiguration"></param>
        internal GoogleReportGenerator(ReportConfiguration reportConfiguration)
        {
            _reportConfiguration = reportConfiguration;
        }


        /// <summary>
        /// Generate report request for google.
        /// </summary>
        /// <returns></returns>
        public List<ReportRequest> Generate()
            => new List<ReportRequest>()
            {
                new ReportRequest
                {
                    DateRanges = new List<DateRange>()
                    {
                        new DateRange
                        {
                            StartDate = _reportConfiguration.ReportDateStart.ToString("yyyy-MM-dd"),
                            EndDate = _reportConfiguration.ReportDateEnd.ToString("yyyy-MM-dd")
                        }
                    },
                    Metrics = _reportConfiguration.Metrics is not null 
                        ? GenerateMetrics(_reportConfiguration.Metrics).ToList() 
                        : throw new ArgumentNullException($"Metrics are null for report '{_reportConfiguration.ReportName}' with view id '{_reportConfiguration.ViewId}'!"),
                    Dimensions = _reportConfiguration.Dimensions is not null
                        ? GenerateDimensions(_reportConfiguration.Dimensions).ToList()
                        : throw new ArgumentNullException($"Dimensions are null for report '{_reportConfiguration.ReportName}' with view id '{_reportConfiguration.ViewId}'!"),
                    ViewId = _reportConfiguration.ViewId.ToString(),
                    FiltersExpression = _reportConfiguration.Filter,
                    PageSize = 10000
                }
            };


        /// <summary>
        /// Dynamically generate dimensions.
        /// </summary>
        /// <param name="dimensions"></param>
        /// <returns></returns>
        private IEnumerable<Dimension> GenerateDimensions(List<Contracts.Dimension> dimensions)
        {
            foreach (Contracts.Dimension dimension in dimensions)
                yield return new Dimension { Name = $"ga:{(dimension.DimensionName.HasValue() ? dimension.DimensionName.Replace("ga:", "") : dimension.DimensionName)}" };
        }


        /// <summary>
        /// Dynamically generate metrics.
        /// </summary>
        /// <param name="metrics"></param>
        /// <returns></returns>
        private IEnumerable<Metric> GenerateMetrics(List<Contracts.Metric> metrics)
        {
            foreach (Contracts.Metric metric in metrics)
                yield return new Metric { Expression = $"ga:{(metric.Expression.HasValue() ? metric.Expression.Replace("ga:", "") : metric.Expression)}", Alias = metric.MetricName };
        }
    }
}