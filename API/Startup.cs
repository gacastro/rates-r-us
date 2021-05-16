using API.Helpers;
using API.Services;
using Main;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace API
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IBuildExchangeRequests, ExchangeRequestBuilder>();
            services.AddScoped<ICalculateExchangeRates, ExchangeRateCalculator>();
            services.AddSingleton<ICacheExchangeRates, ExchangeRatesCache>();
            services.AddSingleton<IRetrieveExchangeRates, ExchangeRatesRetriever>(); // cache is singleton so this one will end up being one as well
            services.AddSingleton<IHaveConfigurations, ApiConfiguration>();
            services.AddHttpClient();
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}