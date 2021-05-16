using System.Collections.Generic;

namespace Main.Models
{
    public class ExchangeRate : IAmExchangeRate
    {
        public IReadOnlyDictionary<Currency, decimal> Rates { get; }
        public long UpdatedAt { get; }

        public ExchangeRate(IReadOnlyDictionary<Currency, decimal> rates, long updatedAt)
        {
            Rates = rates;
            UpdatedAt = updatedAt;
        }
    }
}