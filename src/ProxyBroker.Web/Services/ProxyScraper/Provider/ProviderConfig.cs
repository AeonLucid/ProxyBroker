using System;

namespace ProxyBroker.Web.Services.ProxyScraper.Provider
{
    public class ProviderConfig
    {
        public ProviderConfig(IProvider provider, TimeSpan interval)
        {
            Provider = provider;
            Interval = interval;
        }
        
        public IProvider Provider { get; }
        public TimeSpan Interval { get; }
        public DateTimeOffset? Previous { get; set; }
    }
}