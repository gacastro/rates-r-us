namespace Main.Models
{
    public class ExchangeRequest
    {
        public decimal Price { get; }
        public Currency SourceCurrency { get; }
        public Currency TargetCurrency { get; }

        public ExchangeRequest()
        {
        }
        
        public ExchangeRequest(decimal price, Currency sourceCurrency, Currency targetCurrency)
        {
            Price = price;
            SourceCurrency = sourceCurrency;
            TargetCurrency = targetCurrency;
        }
    }
}