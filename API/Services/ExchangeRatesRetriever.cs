using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Main;
using Main.Models;
using Newtonsoft.Json.Linq;

namespace API.Services
{
    public class ExchangeRatesRetriever : IRetrieveExchangeRates
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly Uri _ratesApiUrl;

        public ExchangeRatesRetriever(IHaveConfigurations configuration, IHttpClientFactory clientFactory)
        {
            _ratesApiUrl = new Uri($"{configuration.RatesApiBaseUrl}/exchangerates/api/latest/");
            _clientFactory = clientFactory;
        }

        public async Task<Dictionary<Currency, IAmExchangeRate>> GetAll()
        {
            var getEurRate = Get(Currency.EUR);
            var getGbpRate = Get(Currency.GBP);
            var getUsdRate = Get(Currency.USD);

            // after this step all tasks are completed
            await Task.WhenAll(new Task[] {getEurRate, getGbpRate, getUsdRate});
            
            var rates = new Dictionary<Currency, IAmExchangeRate>();

            var eurRate = await getEurRate;
            if (eurRate is not EmptyExchangeRate)
            {
                rates.Add(Currency.EUR, eurRate);
            }
            
            var gbpRate = await getGbpRate;
            if (gbpRate is not EmptyExchangeRate)
            {
                rates.Add(Currency.GBP, gbpRate);
            }
            
            var usdRate = await getUsdRate;
            if (usdRate is not EmptyExchangeRate)
            {
                rates.Add(Currency.USD, usdRate);
            }

            return rates;
        }

        public async Task<IAmExchangeRate> Get(Currency sourceCurrency)
        {
            var url = new Uri(_ratesApiUrl, $"{sourceCurrency}.json");
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var client = _clientFactory.CreateClient();

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new EmptyExchangeRate();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonContent = JObject.Parse(responseContent);

            return BuildExchangeRate(jsonContent);
        }

        private static IAmExchangeRate BuildExchangeRate(JObject message)
        {
            // for the MVP purposes I'm assuming the schema is as expected
            // please review my comments in https://github.com/gacastro/rates-r-us#future-improvements
            var rates = new Dictionary<Currency, decimal>
            {
                {Currency.EUR, message["rates"]["EUR"].Value<decimal>()},
                {Currency.GBP, message["rates"]["GBP"].Value<decimal>()},
                {Currency.USD, message["rates"]["USD"].Value<decimal>()}
            };

            return new ExchangeRate(rates, message["time_last_updated"].Value<long>());
        }
    }
}