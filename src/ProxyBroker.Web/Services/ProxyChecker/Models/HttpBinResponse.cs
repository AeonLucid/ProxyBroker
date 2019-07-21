using System.Collections.Generic;
using Newtonsoft.Json;

namespace ProxyBroker.Web.Services.ProxyChecker.Models
{
    public class HttpBinResponse
    {
        [JsonProperty("args")]
        public Dictionary<string, string> Args { get; set; }

        [JsonProperty("headers")]
        public Dictionary<string, string> Headers { get; set; }

        [JsonProperty("origin")]
        public string Origin { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}