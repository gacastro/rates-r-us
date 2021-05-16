using Main.Models;

namespace API.Helpers
{
    public class BuildResult
    {
        public string Error { get; }
        public ExchangeRequest ExchangeRequest { get; }

        public BuildResult(string error, ExchangeRequest exchangeRequest)
        {
            Error = error;
            ExchangeRequest = exchangeRequest;
        }
    }
}