namespace Main.Models
{
    public class EmptyExchangeResponse : IAmExchangeResponse
    {
        public decimal Exchange { get; }
        public string ExchangeIn { get; }
        public decimal ExchangeRate { get; }
    }
}