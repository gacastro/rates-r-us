namespace Main.Models
{
    public class ExchangeResponse : IAmExchangeResponse
    {
        public decimal Exchange { get; }
        public string ExchangeIn { get; }
        public decimal ExchangeRate { get; }

        public ExchangeResponse(decimal exchange, Currency exchangeIn, decimal exchangeRate)
        {
            Exchange = exchange;
            ExchangeIn = exchangeIn.ToString();
            ExchangeRate = exchangeRate;
        }
    }
}