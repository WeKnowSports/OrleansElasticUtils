# OrleansElasticUtils
Orleans Providers for Elastic Search

## How to install
To install OrleansElasticUtils via NuGet, run this command in NuGet package manager console:
```code
PM> Install-Package SBTech.OrleansElasticUtils
```

## StatisticsProvider

**Supports:**
- [x] Silo statistics
- [ ] Client statistics
- [ ] .NET Core

**Configuration for Silo**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<OrleansConfiguration xmlns="urn:orleans">
  <Globals>
    <StatisticsProviders>
      <Provider Type="SBTech.Orleans.Providers.Elastic.ElasticStatisticsProvider"
                Name="ElasticStatisticsProvider"
                ElasticHostAddress="http://localhost:9200"
                ElasticIndex="orleans_statistics"
                ElasticMetricsType="silo_metrics"
                ElasticStatsType="silo_stats" />
    </StatisticsProviders>
  </Globals>
  <Defaults>
    <Statistics ProviderType="ElasticStatisticsProvider" WriteLogStatisticsToTable="true"/>
  </Defaults>
</OrleansConfiguration>
```
