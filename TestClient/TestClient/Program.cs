using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime.Configuration;
using SBTech.Orleans.ElasticUtils;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //ClientConfiguration config = ClientConfiguration.StandardLoad();

            ClientConfiguration config = new ClientConfiguration();

            config.GatewayProvider = ClientConfiguration.GatewayProviderType.Config;
            config.Gateways.Add(new IPEndPoint(IPAddress.Loopback, 30000));


                config.AddElasticSearchStatisticsProvider("ESSP",
                    new Uri("http://smellycat01.devint.dev-r5ead.net:9200"));

            config.StatisticsWriteLogStatisticsToTable = true;
            config.StatisticsCollectionLevel = StatisticsLevel.Verbose3;
            config.StatisticsLogWriteInterval = TimeSpan.FromSeconds(10);
            config.StatisticsMetricsTableWriteInterval = TimeSpan.FromSeconds(10);
            config.StatisticsPerfCountersWriteInterval = TimeSpan.FromSeconds(10);


            Console.ReadLine();
            GrainClient.Initialize(config);
            Console.ReadLine();
            GrainClient.Uninitialize();
        }
    }
}
