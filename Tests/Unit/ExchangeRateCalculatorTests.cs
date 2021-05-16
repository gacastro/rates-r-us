using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Main;
using Main.Models;
using Moq;
using Xunit;

namespace Tests.Unit
{
    // I could use just one method to assert the exchange rates but
    // I believe tests should be clean because after all they are a documentation for your feature
    // As such, preferred to break it by currency and hopefully its clear what is the concern of each test 
    public class ExchangeRateCalculatorTests
    {
        private const decimal TargetEurGbpRate = 0.855552m;
        private const decimal TargetEurUsdRate = 1.183894m;
        private const decimal TargetGbpEurRate = 1.168852m;
        private const decimal TargetGbpUsdRate = 1.384935m;
        private const decimal TargetUsdGbpRate = 0.722077m;
        private const decimal TargetUsdEurRate = 0.844712m;
        private readonly Mock<ICacheExchangeRates> _ratesCache;
        private readonly ICalculateExchangeRates _calculator;
        private readonly IReadOnlyDictionary<Currency, IReadOnlyDictionary<Currency, decimal>> _exchangeRates;

        public ExchangeRateCalculatorTests()
        {
            _ratesCache = new Mock<ICacheExchangeRates>();
            _calculator = new ExchangeRateCalculator(_ratesCache.Object);
            
            var eurExchangeRate = new Dictionary<Currency, decimal>
                {
                    {Currency.EUR, 1m},
                    {Currency.GBP, TargetEurGbpRate},
                    {Currency.USD, TargetEurUsdRate}
                };
            var gbpExchangeRate = new Dictionary<Currency, decimal>
                {
                    {Currency.GBP, 1m},
                    {Currency.EUR, TargetGbpEurRate},
                    {Currency.USD, TargetGbpUsdRate}
                };
            var usdExchangeRate = new Dictionary<Currency, decimal>
                {
                    {Currency.USD, 1m},
                    {Currency.GBP, TargetUsdGbpRate},
                    {Currency.EUR, TargetUsdEurRate}
                };
            _exchangeRates = new Dictionary<Currency, IReadOnlyDictionary<Currency, decimal>>
            {
                {Currency.EUR, eurExchangeRate},
                {Currency.GBP, gbpExchangeRate},
                {Currency.USD, usdExchangeRate}
            };
        }

        [Fact]
        public async Task should_throw_when_cache_fails()
        {
            _ratesCache
                .Setup(cache => cache
                    .Get(It.IsAny<ExchangeRequest>()))
                .ThrowsAsync(new NotSupportedException());

            // I'm not really interested in what is the exception,
            // just want to be sure it gets bubbled up
            await Assert.ThrowsAsync<NotSupportedException>(() => _calculator.Exchange(It.IsAny<ExchangeRequest>()));
        }

        [Fact]
        public async Task returns_empty_when_currency_not_found()
        {
            _ratesCache
                .Setup(cache => cache.Get(It.IsAny<ExchangeRequest>()))
                .ReturnsAsync(new Dictionary<Currency, decimal>());

            var exchangeResponse = await _calculator.Exchange(new ExchangeRequest());

            Assert.IsType<EmptyExchangeResponse>(exchangeResponse);
        }

        public static IEnumerable<object[]> EurData =>
            new List<object[]>
            {
                new object[] { 19.95m, Currency.GBP, TargetEurGbpRate },
                new object[] { 27.61m, Currency.USD, TargetEurUsdRate },
                new object[] { 23.32m, Currency.EUR, 1 }
            };

        [Theory]
        [MemberData(nameof(EurData))]
        public async Task can_exchange_eur(decimal exchange, Currency exchangeIn, decimal exchangeRate)
        {
            var exchangeRequest = new ExchangeRequest(
                23.32m,
                Currency.EUR,
                exchangeIn);
            _ratesCache
                .Setup(cache => cache.Get(exchangeRequest))
                .ReturnsAsync(_exchangeRates[Currency.EUR]);

            var result = await _calculator.Exchange(exchangeRequest);
            
            Assert.Equal(exchange, result.Exchange);
            Assert.Equal(exchangeIn.ToString(), result.ExchangeIn);
            Assert.Equal(exchangeRate, result.ExchangeRate);
        }

        public static IEnumerable<object[]> GbpData =>
            new List<object[]>
            {
                new object[] { 27.26m, Currency.EUR, TargetGbpEurRate },
                new object[] { 32.30, Currency.USD, TargetGbpUsdRate },
                new object[] { 23.32m, Currency.GBP, 1 }
            };

        [Theory]
        [MemberData(nameof(GbpData))]
        public async Task can_exchange_gbp(decimal exchange, Currency exchangeIn, decimal exchangeRate)
        {
            var exchangeRequest = new ExchangeRequest(
                23.32m,
                Currency.GBP,
                exchangeIn);
            _ratesCache
                .Setup(cache => cache.Get(exchangeRequest))
                .ReturnsAsync(_exchangeRates[Currency.GBP]);

            var result = await _calculator.Exchange(exchangeRequest);
            
            Assert.Equal(exchange, result.Exchange);
            Assert.Equal(exchangeIn.ToString(), result.ExchangeIn);
            Assert.Equal(exchangeRate, result.ExchangeRate);
        }

        public static IEnumerable<object[]> UsdData =>
            new List<object[]>
            {
                new object[] { 19.70m, Currency.EUR, TargetUsdEurRate },
                new object[] { 16.84m, Currency.GBP, TargetUsdGbpRate },
                new object[] { 23.32m, Currency.USD, 1 }
            };

        [Theory]
        [MemberData(nameof(UsdData))]
        public async Task can_exchange_usd(decimal exchange, Currency exchangeIn, decimal exchangeRate)
        {
            var exchangeRequest = new ExchangeRequest(
                23.32m,
                Currency.USD,
                exchangeIn);
            _ratesCache
                .Setup(cache => cache.Get(exchangeRequest))
                .ReturnsAsync(_exchangeRates[Currency.USD]);

            var result = await _calculator.Exchange(exchangeRequest);
            
            Assert.Equal(exchange, result.Exchange);
            Assert.Equal(exchangeIn.ToString(), result.ExchangeIn);
            Assert.Equal(exchangeRate, result.ExchangeRate);
        }
    }
}