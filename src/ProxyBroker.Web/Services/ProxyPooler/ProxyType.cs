namespace ProxyBroker.Web.Services.ProxyPooler
{
    public enum ProxyType
    {
        // HTTP_X_FORWARDED_FOR or HTTP_VIA contains real ip
        Transparent = 0,
        // HTTP_X_FORWARDED_FOR or HTTP_VIA contains ip of the proxy or blank
        Anonymous = 1,
        // No identication of proxy usage is sent
        Elite = 2
    }
}