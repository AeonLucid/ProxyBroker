namespace ProxyBroker.Web.Services.ProxyPooler
{
    public interface IProxyPool
    {
        void Put(Proxy proxy);
        bool Has(bool forceChecked = true);
        Proxy Get(bool forceChecked = true);
    }
}