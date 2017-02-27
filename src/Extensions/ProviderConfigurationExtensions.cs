using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Runtime.Configuration;
using SBTech.Orleans.Providers.Elastic;

namespace SBTech.Orleans.ElasticUtils
{
    public static class ProviderConfigurationExtensions
    {
        //
        // Summary:
        //     Adds a storage provider of type Orleans.Storage.MemoryStorage
        //
        // Parameters:
        //   config:
        //     The cluster configuration object to add provider to.
        //
        //   providerName:
        //     The provider name.
        //
        public static void AddElasticSearchStatisticsProvider(this ClusterConfiguration config,
            string providerName, Uri ElasticHostAddress, string ElasticIndex= "orleans_statistics", string ElasticMetricsType= "silo_metrics",
            string ElasticStatsType="silo_stats")
        {
            if (string.IsNullOrWhiteSpace(providerName)) throw new ArgumentNullException(nameof(providerName));
            if (string.IsNullOrWhiteSpace(ElasticIndex)) throw new ArgumentNullException(nameof(ElasticIndex));
            if (string.IsNullOrWhiteSpace(ElasticMetricsType)) throw new ArgumentNullException(nameof(ElasticMetricsType));
            if (string.IsNullOrWhiteSpace(ElasticStatsType)) throw new ArgumentNullException(nameof(ElasticStatsType));

            var properties = new Dictionary<string, string>
            {
                {"ElasticHostAddress", ElasticHostAddress.ToString()},
                {"ElasticIndex", ElasticIndex},
                {"ElasticMetricsType", ElasticMetricsType},
                {"ElasticStatsType", ElasticStatsType}
            };

            config.Globals.RegisterStatisticsProvider<ElasticStatisticsProvider>(providerName, properties);
        }

        public static void AddElasticSearchStatisticsProvider(this ClientConfiguration config,
            string providerName, Uri ElasticHostAddress, string ElasticIndex = "orleans_statistics", string ElasticMetricsType = "silo_metrics",
            string ElasticStatsType = "silo_stats")
        {
            if (string.IsNullOrWhiteSpace(providerName)) throw new ArgumentNullException(nameof(providerName));
            if (string.IsNullOrWhiteSpace(ElasticIndex)) throw new ArgumentNullException(nameof(ElasticIndex));
            if (string.IsNullOrWhiteSpace(ElasticMetricsType)) throw new ArgumentNullException(nameof(ElasticMetricsType));
            if (string.IsNullOrWhiteSpace(ElasticStatsType)) throw new ArgumentNullException(nameof(ElasticStatsType));

            var properties = new Dictionary<string, string>
            {
                {"ElasticHostAddress", ElasticHostAddress.ToString()},
                {"ElasticIndex", ElasticIndex},
                {"ElasticMetricsType", ElasticMetricsType},
                {"ElasticStatsType", ElasticStatsType}
            };

            config.RegisterStatisticsProvider<ElasticStatisticsProvider>(providerName, properties);
        }

    }
}
