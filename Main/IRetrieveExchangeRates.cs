using System.Collections.Generic;
using System.Threading.Tasks;
using Main.Models;

namespace Main
{
    public interface IRetrieveExchangeRates
    {
        Task<IAmExchangeRate> Get(Currency sourceCurrency);
        Task<Dictionary<Currency,IAmExchangeRate>> GetAll();
    }
}