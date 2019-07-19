using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProxyBroker.Web.Services.ProxyPooler;

namespace ProxyBroker.Web.Services.ProxyChecker
{
    public class ProxyCheckerService : BackgroundService
    {
        private const int MaxConcurrency = 3;
        
        private readonly ILogger<ProxyCheckerService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IProxyPool _proxyPool;
        private readonly SemaphoreSlim _concurrencyLock;
        
        public ProxyCheckerService(
            ILogger<ProxyCheckerService> logger, 
            IServiceScopeFactory scopeFactory,
            IProxyPool proxyPool)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _proxyPool = proxyPool;
            _concurrencyLock = new SemaphoreSlim(MaxConcurrency);
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Keep running.
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Keep going if we have unchecked proxies.
                    while (_proxyPool.Has(false))
                    {
                        // Wait until there is room.
                        await _concurrencyLock.WaitAsync(stoppingToken);

                        var proxy = _proxyPool.Get(false);
                        if (proxy != null)
                        {
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await CheckProxyAsync(proxy);
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError(e, "Error when checking proxy.");
                                }
                                finally
                                {
                                    _concurrencyLock.Release();
                                }
                            }, stoppingToken);
                        }
                        else
                        {
                            _concurrencyLock.Release();
                        }
                    }
                    
                    await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
                }
                catch (TaskCanceledException)
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

        private async Task CheckProxyAsync(Proxy proxy)
        {
            _logger.LogInformation(proxy.Ip);
        }
    }
}