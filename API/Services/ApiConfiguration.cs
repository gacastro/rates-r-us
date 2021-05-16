using Main;

namespace API.Services
{
    // configuration should be loaded dynamically per env as stated in https://github.com/gacastro/rates-r-us#future-improvements
    public class ApiConfiguration : IHaveConfigurations
    {
        public byte CacheRefreshRate => 60;
        public string RatesApiBaseUrl => "https://trainlinerecruitment.github.io";
    }
}