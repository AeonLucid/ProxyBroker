using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ProxyBroker.Web.Services.ProxyPooler;

namespace ProxyBroker.Web.Services.ProxyScraper.Provider.Free
{
    public class PatternScraperProvider : IProvider
    {
        private static readonly Regex IpPortPattern = new Regex("(?<ip>(?:(?:25[0-5]|2[0-4]\\d|[01]?\\d\\d?)\\.){3}(?:25[0-5]|2[0-4]\\d|[01]?\\d\\d?))(?=.*?(?:(?:(?:(?:25[0-5]|2[0-4]\\d|[01]?\\d\\d?)\\.){3}(?:25[0-5]|2[0-4]\\d|[01]?\\d\\d?))|(?<port>\\d{2,5})))", RegexOptions.Compiled | RegexOptions.Singleline);
        
        private readonly string _url;
        private readonly Regex _pattern;

        public PatternScraperProvider(string url) : this(url, IpPortPattern)
        {
            _url = url;
        }

        public PatternScraperProvider(string url, Regex pattern)
        {
            _url = url;
            _pattern = pattern;
        }

        public string GetName()
        {
            return _url;
        }

        public async Task<HashSet<Proxy>> GetProxiesAsync(HttpClient client)
        {
            var results = new HashSet<Proxy>();
            var data = await client.GetStringAsync(_url);
            var matches = _pattern.Matches(data);
            
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups["port"].Value, out var port))
                {
                    results.Add(new Proxy(match.Groups["ip"].Value, port));
                }
            }

            return results;
        }
    }
}