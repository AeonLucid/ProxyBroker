using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ProxyBroker.Web.Services.ProxyPooler
{
    public class ProxyPool : IProxyPool
    {
        private readonly ILogger<ProxyPool> _logger;

        private readonly object _writeLock;
        private readonly HashSet<Proxy> _proxies;
        private readonly ConcurrentQueue<Proxy> _proxiesChecked;
        private readonly ConcurrentQueue<Proxy> _proxiesUnchecked;

        public ProxyPool(ILogger<ProxyPool> logger)
        {
            _logger = logger;
            _writeLock = new object();
            _proxies = new HashSet<Proxy>();
            _proxiesChecked = new ConcurrentQueue<Proxy>();
            _proxiesUnchecked = new ConcurrentQueue<Proxy>();
        }

        public void Put(Proxy proxy)
        {
            lock (_writeLock)
            {
                // Check if we already have this proxy in the pool.
                if (_proxies.Contains(proxy))
                {
                    return;
                }

                // Add to the pool.
                _proxies.Add(proxy);
            }

            if (proxy.Checked)
            {
                _proxiesChecked.Enqueue(proxy);
            }
            else
            {
                _proxiesUnchecked.Enqueue(proxy);
            }
        }

        public Proxy Get(bool forceChecked = false)
        {
            Proxy result;
            
            var found = forceChecked
                ? _proxiesChecked.TryDequeue(out result)
                : _proxiesUnchecked.TryDequeue(out result);

            return found 
                ? result 
                : null;
        }
    }
}