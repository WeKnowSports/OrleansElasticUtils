using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.Providers;
using Nest;
using Orleans.Runtime.Configuration;

namespace SBTech.Orleans.Providers.Elastic
{
    public class ElasticClientMetricsProvider : IClientMetricsDataPublisher,
                                             IStatisticsPublisher,
                                             IProvider
    {
        private string _elasticHostAddress;
        private string _elasticIndex { get; set; } = "orleans_statistics";
        private string _elasticType { get; set; } = "metrics";


        private string ElasticHostAddress() => _elasticHostAddress;
        private string ElasticIndex() => _elasticIndex + "-" + DateTime.UtcNow.ToString("yyyy-MM-dd-HH");
        private string ElasticType() => _elasticType;


        // Example: 2010-09-02 09:50:43.341 GMT - Variant of UniversalSorta­bleDateTimePat­tern
        const string DATE_TIME_FORMAT = "yyyy-MM-dd-" + "HH:mm:ss.fff 'GMT'";        
        int MAX_BULK_UPDATE_DOCS = 200;
        State _state = new State();
        Logger _logger;   

        /// <summary>
        /// Name of the provider
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Closes provider
        /// </summary>        
        public Task Close() => TaskDone.Done;

        /// <summary>
        /// Initialization of ElasticStatisticsProvider
        /// </summary>        
        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Name = name;
            _state.Id = providerRuntime.SiloIdentity;
            _logger = providerRuntime.GetLogger(typeof(ElasticStatisticsProvider).Name);

            if (config.Properties.ContainsKey("ElasticHostAddress"))
                _elasticHostAddress = config.Properties["ElasticHostAddress"];

            if (config.Properties.ContainsKey("ElasticIndex"))
                _elasticIndex = config.Properties["ElasticIndex"];

            if (config.Properties.ContainsKey("ElasticType"))
                _elasticType = config.Properties["ElasticType"];


            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(_elasticHostAddress))
                throw new ArgumentNullException("ElasticHostAddress");
            if (string.IsNullOrWhiteSpace(_elasticIndex))
                throw new ArgumentNullException("ElasticIndex");
            if (string.IsNullOrWhiteSpace(_elasticType))
                throw new ArgumentNullException("ElasticType");


            return TaskDone.Done;
        }

        //public Task Init(string deploymentId, string storageConnectionString, SiloAddress siloAddress, string siloName, IPEndPoint gateway, string hostName) => TaskDone.Done;

        public Task Init(bool isSilo, string storageConnectionString, string deploymentId, string address, string siloName, string hostName) => TaskDone.Done;

        /// <summary>
        /// Initialization of configuration for Silo
        /// </summary>        
        public void AddConfiguration(string deploymentId, bool isSilo, string siloName, SiloAddress address, IPEndPoint gateway, string hostName)
        {            
            _state.DeploymentId = deploymentId;
            _state.IsSilo = isSilo;
            _state.SiloName = siloName;
            _state.Address = address.ToString();
            _state.GatewayAddress = gateway.ToString();
            _state.HostName = hostName;            
        }

        /// <summary>
        /// Metrics for Silo
        /// </summary>        
        public async Task ReportMetrics(ISiloPerformanceMetrics metricsData)
        {
            if (_logger != null && _logger.IsVerbose3)
                _logger.Verbose3("ElasticStatisticsProvider.ReportMetrics called with metrics: {0}, name: {1}, id: {2}.", metricsData, _state.SiloName, _state.Id);

            try
            {
                var esClient = CreateElasticClient(ElasticHostAddress());

                var siloMetrics = PopulateSiloMetricsEntry(metricsData, _state);

                var response = await esClient.IndexAsync(siloMetrics, (ds) => ds.Index(ElasticIndex())
                                                                                .Type(ElasticType()));

                if (!response.IsValid && _logger != null && _logger.IsVerbose)
                    _logger.Verbose(response.ServerError.Status, response.ServerError.Error);
            }
            catch (Exception ex)
            {
                if (_logger != null && _logger.IsVerbose)
                    _logger.Verbose("ElasticStatisticsProvider.ReportMetrics failed: {0}", ex);

                throw;
            }
        }

        /// <summary>
        /// Stats for Silo and Client
        /// </summary>  
        public async Task ReportStats(List<ICounter> statsCounters)
        {
            if (_logger != null && _logger.IsVerbose3)
                _logger.Verbose3("ElasticStatisticsProvider.ReportStats called with {0} counters, name: {1}, id: {2}", statsCounters.Count, _state.SiloName, _state.Id);

            try
            {
                var esClient = CreateElasticClient(ElasticHostAddress());

                var counterBatches = statsCounters.Where(cs => cs.Storage == CounterStorage.LogAndTable)
                                                  .OrderBy(cs => cs.Name)
                                                  .Select(cs => PopulateStatsTableEntry(cs, _state))
                                                  .BatchIEnumerable(MAX_BULK_UPDATE_DOCS);

                foreach (var batch in counterBatches)
                {
                    var bulkDesc = new BulkDescriptor();
                    bulkDesc.CreateMany(batch, (bulk, q) => bulk.Index(ElasticIndex())
                                                                .Type(ElasticType()));

                    var response = await esClient.BulkAsync(bulkDesc);

                    if (response.Errors && _logger != null && _logger.IsVerbose)
                        _logger.Error(response.ServerError.Status, response.ServerError.Error);
                }
            }
            catch (Exception ex)
            {
                if (_logger != null && _logger.IsVerbose)
                    _logger.Verbose("ElasticStatisticsProvider.ReportStats failed: {0}", ex);

                throw;
            }
        }


        static ElasticClient CreateElasticClient(string elasticHostAddress)
        {            
            var node = new Uri(elasticHostAddress);
            return new ElasticClient(new ConnectionSettings(node));
        }       

        static SiloMetricsEntry PopulateSiloMetricsEntry(ISiloPerformanceMetrics metricsData, State state)
        {
            return new SiloMetricsEntry
            {
                SiloId = state.Id,
                SiloName = state.SiloName,
                DeploymentId = state.DeploymentId,
                Address = state.Address,
                HostName = state.HostName,
                GatewayAddress = state.GatewayAddress,
                CpuUsage = metricsData.CpuUsage,
                TotalPhysicalMemory = metricsData.TotalPhysicalMemory,
                AvailablePhysicalMemory = metricsData.AvailablePhysicalMemory,
                MemoryUsage = metricsData.MemoryUsage,
                SendQueueLength = metricsData.SendQueueLength,
                ReceiveQueueLength = metricsData.ReceiveQueueLength,
                SentMessages = metricsData.SentMessages,
                ReceivedMessages = metricsData.ReceivedMessages,
                ActivationsCount = metricsData.ActivationCount,
                RecentlyUsedActivations = metricsData.RecentlyUsedActivationCount,
                RequestQueueLength = metricsData.ReceiveQueueLength,
                IsOverloaded = metricsData.IsOverloaded,
                ClientCount = metricsData.ClientCount,
                Time = DateTime.UtcNow.ToString(DATE_TIME_FORMAT, CultureInfo.InvariantCulture)                
            };
        }

        static StatsTableEntry PopulateStatsTableEntry(ICounter counter, State state)
        {
            return new StatsTableEntry
            {
                Identity = state.Id,
                DeploymentId = state.DeploymentId,                
                Name = state.SiloName,
                HostName = state.HostName,
                Statistic = counter.Name,
                IsDelta = counter.IsValueDelta,
                StatValue = counter.IsValueDelta ? counter.GetDeltaString() : counter.GetValueString(),
                Time = DateTime.UtcNow.ToString(DATE_TIME_FORMAT, CultureInfo.InvariantCulture)
            };
        }

        public Task Init(ClientConfiguration config, IPAddress address, string clientId)
        {
            throw new NotImplementedException();
        }

        public Task ReportMetrics(IClientPerformanceMetrics metricsData)
        {
            throw new NotImplementedException();
        }
    }

    //class State
    //{
    //    public string DeploymentId { get; set; } = "";
    //    public bool IsSilo { get; set; } = true;
    //    public string SiloName { get; set; } = "";
    //    public string Id { get; set; } = "";
    //    public string Address { get; set; } = "";
    //    public string GatewayAddress { get; set; } = "";
    //    public string HostName { get; set; } = "";
    //    public string ElasticHostAddress { get; set; } = "http://localhost:9200";
    //    public string ElasticIndex { get; set; } = "orleans_statistics";
    //    public string ElasticMetricsType { get; set; } = "metrics";
    //    public string ElasticStatsType { get; set; } = "stats";
    //}

    //[Serializable]
    //internal class StatsTableEntry
    //{
    //    public string Identity { get; set; }        
    //    public string DeploymentId { get; set; }        
    //    public string Name { get; set; }        
    //    public string HostName { get; set; }
    //    public string Statistic { get; set; }
    //    public string StatValue { get; set; }
    //    public bool IsDelta { get; set; }
    //    public string Time { get; set; }
    //    public override string ToString() => $"StatsTableEntry[ Identity={Identity} DeploymentId={DeploymentId} Name={Name} HostName={HostName} Statistic={Statistic} StatValue={StatValue} IsDelta={IsDelta} Time={Time} ]";
    //}

    //[Serializable]
    //internal class SiloMetricsEntry
    //{
    //    public string SiloId { get; set; }
    //    public string SiloName { get; set; }
    //    public string DeploymentId { get; set; }
    //    public string Address { get; set; }
    //    public string HostName { get; set; }
    //    public string GatewayAddress { get; set; }
    //    public double CpuUsage { get; set; }
    //    public long TotalPhysicalMemory { get; set; }
    //    public long AvailablePhysicalMemory { get; set; }        
    //    public long MemoryUsage { get; set; }
    //    public int SendQueueLength { get; set; }
    //    public int ReceiveQueueLength { get; set; }
    //    public long SentMessages { get; set; }
    //    public long ReceivedMessages { get; set; }
    //    public int ActivationsCount { get; set; }
    //    public int RecentlyUsedActivations { get; set; }
    //    public int RequestQueueLength { get; set; }
    //    public bool IsOverloaded { get; set; }
    //    public long ClientCount { get; set; }
    //    public string Time { get; set; }
    //    public override string ToString() => $"SiloMetricsEntry[ SiloId={SiloId} DeploymentId={DeploymentId} Address={Address} HostName={HostName} GatewayAddress={GatewayAddress} CpuUsage={CpuUsage} TotalPhysicalMemory={TotalPhysicalMemory} AvailablePhysicalMemory={AvailablePhysicalMemory} MemoryUsage={MemoryUsage} SendQueueLength={SendQueueLength} ReceiveQueueLength={ReceiveQueueLength} SentMessages={SentMessages} ReceivedMessages={ReceivedMessages} ActivationsCount={ActivationsCount} RecentlyUsedActivations={RecentlyUsedActivations} RequestQueueLength={RequestQueueLength} IsOverloaded={IsOverloaded} ClientCount={ClientCount} Time={Time} ]";
    //}
}
