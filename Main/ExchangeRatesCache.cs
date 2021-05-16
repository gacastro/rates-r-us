using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Main.Models;

namespace Main
{
    // this is a very very simple cache to be used only for MVP
    // please review my comments in https://github.com/gacastro/rates-r-us#future-improvements 
    public class ExchangeRatesCache : ICacheExchangeRates
    {
        private readonly IHaveConfigurations _configuration;
        private readonly IRetrieveExchangeRates _exchangeRatesRetriever;
        private readonly Dictionary<Currency, IAmExchangeRate> _cache;

        public ExchangeRatesCache(IHaveConfigurations configuration, IRetrieveExchangeRates exchangeRatesRetriever)
        {
            _configuration = configuration;
            _exchangeRatesRetriever = exchangeRatesRetriever;

            _cache = new Dictionary<Currency, IAmExchangeRate>();
        }

        public async Task<IReadOnlyDictionary<Currency, decimal>> Get(ExchangeRequest exchangeRequest)
        {
            await RefreshCache(exchangeRequest.SourceCurrency);

            return _cache.ContainsKey(exchangeRequest.SourceCurrency)
                ? _cache[exchangeRequest.SourceCurrency].Rates
                : new Dictionary<Currency, decimal>();
        }

        private async Task RefreshCache(Currency currencyToRefresh)
        {
            if (_cache.Count == 0)
            {
                var newExchangeRates = await _exchangeRatesRetriever.GetAll();
                foreach (var (currency, rate) in newExchangeRates)
                {
                    _cache.Add(currency, rate);
                }
                
                return;
            }

            // this can happen when the above method does not load the currency because it was not found
            var hasCurrency = _cache.TryGetValue(currencyToRefresh, out var exchangeRate);
            if (!hasCurrency)
            {
                return;
            }
            
            var cacheIsFreshFrom = DateTimeOffset.UtcNow.AddDays(-_configuration.CacheRefreshRate).ToUnixTimeSeconds();
            var cacheHasExpired = exchangeRate.UpdatedAt < cacheIsFreshFrom;

            if (cacheHasExpired)
            {
                var newExchangeRate = await _exchangeRatesRetriever.Get(currencyToRefresh);
                if (newExchangeRate is EmptyExchangeRate)
                {
                    // could not retrieve fresher info
                    // so its best to remove it and signal this as an issue
                    // please review the comment in https://github.com/gacastro/rates-r-us#future-improvements
                    _cache.Remove(currencyToRefresh);
                    return;
                }

                _cache[currencyToRefresh] = newExchangeRate;
            }
        }
    }
}