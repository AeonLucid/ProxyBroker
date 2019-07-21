using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProxyBroker.Web.Services.ProxyChecker.Models;
using ProxyBroker.Web.Services.ProxyPooler;

namespace ProxyBroker.Web.Services.ProxyChecker
{
    public class ProxyCheckerService : BackgroundService
    {
        private const int MaxConcurrency = 8;
        
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
            var httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = false,
                Proxy = new WebProxy(proxy.Ip, proxy.Port)
            };

            using (var httpClient = new HttpClient(httpClientHandler, true))
            {
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var responseStr = await httpClient.GetStringAsync("http://hb.opencpu.org/get?show_env");
                    var responseTime = stopwatch.ElapsedMilliseconds;
                    var response = JsonConvert.DeserializeObject<HttpBinResponse>(responseStr);

                    proxy.Checked = true;
                    proxy.ResponseTime = responseTime;
                    proxy.Protocol = ProxyProtocol.HTTP;

                    var headers = response.Headers.ToDictionary(x => x.Key.ToLower(), y => y.Value);
                    if (headers.ContainsKey("x-forwarded-for"))
                    {
                        // TODO: Need to check for real ip.
                        proxy.Type = ProxyType.Transparent;
                    }
                    else if (headers.ContainsKey("via"))
                    {
                        // TODO: Need to check for real ip.
                        proxy.Type = ProxyType.Transparent;
                    }
                    else
                    {
                        proxy.Type = ProxyType.Elite;
                    }

                    _proxyPool.Put(proxy);
                    
                    _logger.LogDebug("[{0}] Works, took {1}ms", proxy, proxy.ResponseTime);
                }
                catch (Exception e)
                {
                    _logger.LogDebug("[{0}] Exception: {1}", proxy, e.GetType().Name);
                    return;
                }
            }
        }
    }
}