namespace Main.Models
{
    public interface IAmExchangeResponse
    {
        decimal Exchange { get; }
        string ExchangeIn { get; }
        decimal ExchangeRate { get; }
    }
}