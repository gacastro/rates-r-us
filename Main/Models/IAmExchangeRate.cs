using System.Collections.Generic;

namespace Main.Models
{
    public interface IAmExchangeRate
    {
        // this property started of as an object but the more I looked the more it seemed like a dictionary
        // but happy to be convinced otherwise
        IReadOnlyDictionary<Currency, decimal> Rates { get; }
        long UpdatedAt { get; }
    }
}