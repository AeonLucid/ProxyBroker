using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ProxyBroker.Web.Services.ProxyPooler;

namespace ProxyBroker.Web.Services.ProxyScraper.Provider
{
    public interface IProvider
    {
        string GetName();
        
        Task<HashSet<Proxy>> GetProxiesAsync(HttpClient client);
    }
}