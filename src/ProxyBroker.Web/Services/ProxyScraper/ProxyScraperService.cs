using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProxyBroker.Web.Services.ProxyPooler;
using ProxyBroker.Web.Services.ProxyScraper.Provider;
using ProxyBroker.Web.Services.ProxyScraper.Provider.Free;

namespace ProxyBroker.Web.Services.ProxyScraper
{
    public class ProxyScraperService : BackgroundService
    {
        private const int MaxConcurrency = 3;
        
        private readonly ILogger<ProxyScraperService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IProxyPool _proxyPool;
        private readonly IReadOnlyList<ProviderConfig> _providers;

        public ProxyScraperService(
            ILogger<ProxyScraperService> logger, 
            IServiceScopeFactory scopeFactory,
            IProxyPool proxyPool)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _proxyPool = proxyPool;
            _providers = new List<ProviderConfig>
            {
                new ProviderConfig(
                    new PatternScraperProvider("https://www.ipaddress.com/proxy-list/"), 
                    TimeSpan.FromHours(6)),
                
                new ProviderConfig(
                    new PatternScraperProvider("https://www.sslproxies.org/"), 
                    TimeSpan.FromHours(1)),
                
                new ProviderConfig(
                    new PatternScraperProvider("https://hugeproxies.com/home/"), 
                    TimeSpan.FromHours(1)),
                
                new ProviderConfig(
                    new PatternScraperProvider("https://api.proxyscrape.com/?request=getproxies&proxytype=http"), 
                    TimeSpan.FromHours(1)),
            };
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Fetch outdated providers.
                    var time = DateTimeOffset.UtcNow;
                    var providerConfigs = _providers.Where(x => 
                        x.Previous.HasValue == false || 
                        x.Previous - time > x.Interval);
                    
                    using (var concurrencyLock = new SemaphoreSlim(MaxConcurrency))
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                        var scrapeTasks = new List<Task>();
                        
                        // Fetch from every provider.
                        foreach (var providerConfig in providerConfigs)
                        {
                            // Wait until there is room.
                            await concurrencyLock.WaitAsync(stoppingToken);
                            
                            var task = Task.Run(async () =>
                            {
                                var name = providerConfig.Provider.GetName();
                                var client = httpClientFactory.CreateClient("Scraper");

                                try
                                {
                                    var proxies = await providerConfig.Provider.GetProxiesAsync(client);

                                    foreach (var proxy in proxies)
                                    {
                                        _proxyPool.Put(proxy);
                                    }
                                    
                                    _logger.LogDebug("Scraped {0} from {1}.", proxies.Count, name);
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError(e, "Error in {0}.", name);
                                }
                                finally
                                {
                                    providerConfig.Previous = time;
                                    
                                    // Allow another provider to be scraped.
                                    concurrencyLock.Release();
                                }
                            }, stoppingToken);
                            
                            scrapeTasks.Add(task);
                        }

                        await Task.WhenAll(scrapeTasks);
                    }
                
                    // Wait 5 seconds.
                    await Task.Delay(5000, stoppingToken);
                }
                catch (TaskCanceledException e)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        // Graceful shutdown.
                        break;
                    }

                    throw;
                }
            }
        }
    }
}