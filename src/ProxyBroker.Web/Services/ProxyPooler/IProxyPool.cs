namespace ProxyBroker.Web.Services.ProxyPooler
{
    public interface IProxyPool
    {
        void Put(Proxy proxy);
        Proxy Get(bool forceChecked = false);
    }
}