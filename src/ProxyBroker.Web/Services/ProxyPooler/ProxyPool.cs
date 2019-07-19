using Microsoft.Extensions.Logging;

namespace ProxyBroker.Web.Services.ProxyPooler
{
    public class ProxyPool : IProxyPool
    {
        private readonly ILogger<ProxyPool> _logger;

        public ProxyPool(ILogger<ProxyPool> logger)
        {
            _logger = logger;
        }

        public void Add(Proxy proxy)
        {
            throw new System.NotImplementedException();
        }
    }
}