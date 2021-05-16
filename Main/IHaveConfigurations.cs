namespace Main
{
    public interface IHaveConfigurations
    {
        byte CacheRefreshRate { get; }
        string RatesApiBaseUrl { get; }
    }
}