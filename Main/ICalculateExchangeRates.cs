using System.Threading.Tasks;
using Main.Models;

namespace Main
{
    public interface ICalculateExchangeRates
    {
        Task<IAmExchangeResponse> Exchange(ExchangeRequest exchangeRequest);
    }
}