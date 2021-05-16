using System.Collections.Generic;
using System.Threading.Tasks;
using Main.Models;

namespace Main
{
    public interface ICacheExchangeRates
    {
        Task<IReadOnlyDictionary<Currency, decimal>> Get(ExchangeRequest exchangeRequest);
    }
}