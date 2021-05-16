using System;
using System.Collections.Generic;
using API.Models;
using Main.Models;

namespace API.Helpers
{
    public class ExchangeRequestBuilder : IBuildExchangeRequests
    {
        private RequestInfo _info;
        private Currency _sourceCurrency;
        private Currency _targetCurrency;

        public BuildResult Build(RequestInfo info)
        {
            _info = info;
            
            var priceIsWrong = _info.Price <= 0;
            if (priceIsWrong)
            {
                return new BuildResult( 
                    "The price is missing or its not a value greater than 0",
                    new ExchangeRequest());
            }
            
            var missingCurrencies = FindMissingCurrencies();
            if (missingCurrencies.Count > 0)
            {
                return new BuildResult(
                    $"The following currencies are missing from the input: '{string.Join(',', missingCurrencies)}'.",
                    new ExchangeRequest());
            }

            var invalidCurrencies = TryToSetCurrencies();
            if (invalidCurrencies.Count > 0)
            {
                return new BuildResult(
                    $"The following currencies are invalid: '{string.Join(',', invalidCurrencies)}'. " +
                    "Please remember that the source or target currency has to be one of: eur, gbp, usd",
                    new ExchangeRequest());
            }

            return new BuildResult(
                string.Empty,
                new ExchangeRequest(_info.Price, _sourceCurrency, _targetCurrency));
        }

        private List<string> FindMissingCurrencies()
        {
            var missing = new List<string>();

            if (string.IsNullOrWhiteSpace(_info.Source))
            {
                missing.Add(nameof(_info.Source));
            }

            if (string.IsNullOrWhiteSpace(_info.Target))
            {
                missing.Add(nameof(_info.Target));
            }

            return missing;
        }
        
        private List<string> TryToSetCurrencies()
        {
            var invalid = new List<string>();

            var isSourceCurrency = Enum.TryParse(_info.Source, true, out Currency sourceCurrency);
            if (!isSourceCurrency)
            {
                invalid.Add(nameof(_info.Source));
            }
            else
            {
                _sourceCurrency = sourceCurrency;
            }

            var isTargetCurrency = Enum.TryParse(_info.Target, true, out Currency targetCurrency);
            if (!isTargetCurrency)
            {
                invalid.Add(nameof(_info.Target));
            }
            else
            {
                _targetCurrency = targetCurrency;
            }

            return invalid;
        }
    }
}