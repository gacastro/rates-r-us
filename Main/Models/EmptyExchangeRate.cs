using System.Collections.Generic;

namespace Main.Models
{
    public class EmptyExchangeRate : IAmExchangeRate
    {
        public IReadOnlyDictionary<Currency, decimal> Rates { get; }
        public long UpdatedAt { get; }
    }
}