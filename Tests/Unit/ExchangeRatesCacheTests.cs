using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Main;
using Main.Models;
using Moq;
using Xunit;

namespace Tests.Unit
{
    public class ExchangeRatesCacheTests
    {
        private const decimal TargetEurGbpRate = 0.855552m;
        private const decimal TargetEurUsdRate = 1.183894m;

        private readonly ICacheExchangeRates _cache;
        private readonly ExchangeRate _eurExchangeRate;
        private readonly Mock<IHaveConfigurations> _configuration;
        private readonly Mock<IRetrieveExchangeRates> _exchangeRatesRetriever;
        private readonly long _updatedAt = DateTimeOffset.UtcNow.AddDays(-10).ToUnixTimeSeconds();

        public ExchangeRatesCacheTests()
        {
            _configuration = new Mock<IHaveConfigurations>();
            _exchangeRatesRetriever = new Mock<IRetrieveExchangeRates>();
            _cache = new ExchangeRatesCache(_configuration.Object, _exchangeRatesRetriever.Object);
            
            _eurExchangeRate = new ExchangeRate(
                new Dictionary<Currency, decimal>
                {
                    {Currency.EUR, 1m},
                    {Currency.GBP, TargetEurGbpRate},
                    {Currency.USD, TargetEurUsdRate}
                },
                _updatedAt);
        }

        [Fact]
        public async Task should_throw_when_retriever_fails_to_load()
        {
            _exchangeRatesRetriever
                .Setup(retriever => 
                    retriever.GetAll())
                .ThrowsAsync(new NotSupportedException());

            // I'm not really interested in what is the exception,
            // just want to be sure it gets bubbled up
            await Assert.ThrowsAsync<NotSupportedException>(() => _cache.Get(new ExchangeRequest()));
        }

        [Fact]
        public async Task unloaded_cache_returns_empty_when_currency_not_found()
        {
            _exchangeRatesRetriever
                .Setup(retriever => 
                    retriever.GetAll())
                .ReturnsAsync(new Dictionary<Currency, IAmExchangeRate>());

            var cache = await _cache.Get(new ExchangeRequest());
            
            Assert.Empty(cache);
        }

        [Fact]
        public async Task can_load_cache()
        {
            var exchangeRequest = new ExchangeRequest(23.32m, Currency.EUR, Currency.GBP);
            var cacheToLoad = new Dictionary<Currency, IAmExchangeRate>
            {
                {Currency.EUR, _eurExchangeRate }
            };
            _exchangeRatesRetriever
                .Setup(retriever => 
                    retriever.GetAll())
                .ReturnsAsync(cacheToLoad);

            var eurRates = await _cache.Get(exchangeRequest);
            
            Assert.Equal(3, eurRates.Count);
            Assert.Equal(cacheToLoad[Currency.EUR].Rates[Currency.EUR], eurRates[Currency.EUR]);
            Assert.Equal(cacheToLoad[Currency.EUR].Rates[Currency.GBP], eurRates[Currency.GBP]);
            Assert.Equal(cacheToLoad[Currency.EUR].Rates[Currency.USD], eurRates[Currency.USD]);
        }

        [Fact]
        public async Task incomplete_cache_returns_empty_for_the_missing_currency()
        {
            var loadingExchangeRequest = new ExchangeRequest(23.32m, Currency.EUR, Currency.GBP);
            var notFoundExchangeRequest = new ExchangeRequest(23.32m, Currency.GBP, Currency.USD);
            await LoadCache(loadingExchangeRequest);

            var cache = await _cache.Get(notFoundExchangeRequest);
            
            Assert.Empty(cache);
        }

        [Fact]
        public async Task should_throw_when_retriever_fails_to_refresh()
        {
            var exchangeRequest = new ExchangeRequest(23.32m, Currency.EUR, Currency.GBP);
            await LoadCache(exchangeRequest);
            _configuration
                .Setup(config => config.CacheRefreshRate)
                .Returns(1);
            _exchangeRatesRetriever
                .Setup(retriever => 
                    retriever.Get(exchangeRequest.SourceCurrency))
                .ThrowsAsync(new OutOfMemoryException());
            
            // I'm not really interested in what is the exception,
            // just want to be sure it gets bubbled up
            await Assert.ThrowsAsync<OutOfMemoryException>(() => _cache.Get(exchangeRequest));
        }

        [Fact]
        public async Task returns_empty_when_currency_not_found_while_refreshing()
        {
            var exchangeRequest = new ExchangeRequest(23.32m, Currency.EUR, Currency.GBP);
            await LoadCache(exchangeRequest);
            _configuration
                .Setup(config => config.CacheRefreshRate)
                .Returns(1);
            _exchangeRatesRetriever
                .Setup(retriever => 
                    retriever.Get(exchangeRequest.SourceCurrency))
                .ReturnsAsync(new EmptyExchangeRate());

            var exchangeResult = await _cache.Get(exchangeRequest);
            
            Assert.Empty(exchangeResult);
        }
        
        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task can_refresh_cache(byte cacheRefreshRate)
        {
            var exchangeRequest = new ExchangeRequest(23.32m, Currency.EUR, Currency.GBP);
            await LoadCache(exchangeRequest);

            var freshTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); 
            var freshExchangeRate = new ExchangeRate(
                new Dictionary<Currency, decimal>
                {
                    {Currency.EUR, 1m},
                    {Currency.GBP, 3.14m},
                    {Currency.USD, 2.78m}
                },
                freshTime);
            _configuration
                .Setup(config => config.CacheRefreshRate)
                .Returns(cacheRefreshRate);
            _exchangeRatesRetriever
                .Setup(retriever => 
                    retriever.Get(exchangeRequest.SourceCurrency))
                .ReturnsAsync(freshExchangeRate);
            
            var eurRates = await _cache.Get(exchangeRequest);
            
            Assert.Equal(3, eurRates.Count);
            Assert.Equal(freshExchangeRate.Rates[Currency.EUR], eurRates[Currency.EUR]);
            Assert.Equal(freshExchangeRate.Rates[Currency.GBP], eurRates[Currency.GBP]);
            Assert.Equal(freshExchangeRate.Rates[Currency.USD], eurRates[Currency.USD]);
        }

        [Fact]
        public async Task can_read_from_cache_without_refreshing()
        {
            var exchangeRequest = new ExchangeRequest(23.32m, Currency.EUR, Currency.GBP);
            await LoadCache(exchangeRequest, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            _configuration
                .Setup(config => config.CacheRefreshRate)
                .Returns(1);
            _exchangeRatesRetriever
                .Setup(retriever => 
                    retriever.Get(exchangeRequest.SourceCurrency))
                .ThrowsAsync(new OutOfMemoryException()); //to ensure we're not going getting fresher currencies

            var eurRates = await _cache.Get(exchangeRequest);
            
            Assert.Equal(3, eurRates.Count);
            Assert.Equal(_eurExchangeRate.Rates[Currency.EUR], eurRates[Currency.EUR]);
            Assert.Equal(_eurExchangeRate.Rates[Currency.GBP], eurRates[Currency.GBP]);
            Assert.Equal(_eurExchangeRate.Rates[Currency.USD], eurRates[Currency.USD]);
        }

        private async Task LoadCache(ExchangeRequest exchangeRequest, long cacheUpdateAt = 0)
        {
            var cacheToLoad =
                cacheUpdateAt == 0 
                    ? new Dictionary<Currency, IAmExchangeRate>
                        {
                            {Currency.EUR, _eurExchangeRate}
                        }
                    : new Dictionary<Currency, IAmExchangeRate>
                    {
                            {Currency.EUR, new ExchangeRate(_eurExchangeRate.Rates, cacheUpdateAt)}
                        };
            _exchangeRatesRetriever
                .Setup(retriever =>
                    retriever.GetAll())
                .ReturnsAsync(cacheToLoad);
            
            await _cache.Get(exchangeRequest);
        }
    }
}