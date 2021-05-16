using System;
using System.Threading.Tasks;
using Main.Models;

namespace Main
{
    public class ExchangeRateCalculator : ICalculateExchangeRates
    {
        private readonly ICacheExchangeRates _ratesCache;

        public ExchangeRateCalculator(ICacheExchangeRates ratesCache)
        {
            _ratesCache = ratesCache;
        }

        public async Task<IAmExchangeResponse> Exchange(ExchangeRequest exchangeRequest)
        {
            var rates = await _ratesCache.Get(exchangeRequest);

            if (rates.Count == 0)
            {
                return new EmptyExchangeResponse();
            }

            var valueToExchange = exchangeRequest.Price;
            var targetRate = rates[exchangeRequest.TargetCurrency];
            var referenceRate = rates[exchangeRequest.SourceCurrency];

            // As an MVP do not expect to operate over large values
            // As such, not performing the calculation under a checked context
            // please review the comment in https://github.com/gacastro/rates-r-us#future-improvements
            var exchange = valueToExchange * targetRate / referenceRate;
            var roundedExchange = Math.Round(exchange, 2);

            return new ExchangeResponse(roundedExchange, exchangeRequest.TargetCurrency, targetRate);
        }
    }
}