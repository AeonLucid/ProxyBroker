using System;
using System.Threading.Tasks;

namespace ProxyBroker.Web.Services.ProxyPooler
{
    public class Proxy
    {
        public Proxy(string ip, int port)
        {
            Ip = ip;
            Port = port;
        }

        public string Ip { get; }
        public int Port { get; }
        public ProxyProtocol Protocol { get; set; }
        
        public async Task ConnectAsync()
        {
            
        }

        public async Task NegotiateAsync()
        {
            
        }
        
        public async Task CloseAsync()
        {
            
        }

        protected bool Equals(Proxy other)
        {
            return string.Equals(Ip, other.Ip, StringComparison.OrdinalIgnoreCase) && Port == other.Port;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(Proxy)) return false;
            return Equals((Proxy) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.OrdinalIgnoreCase.GetHashCode(Ip) * 397) ^ Port;
            }
        }
    }
}