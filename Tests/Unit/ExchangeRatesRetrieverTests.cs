using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using API.Services;
using Main;
using Main.Models;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Tests.Unit
{
    public class ExchangeRatesRetrieverTests
    {
        private const string JsonEurRate = 
            @"{
	            ""base"": ""EUR"",
	            ""date"": ""2021-04-07"",
	            ""time_last_updated"": 1617753602,
	            ""rates"": {
		            ""GBP"": 0.855552,
		            ""EUR"": 1,
		            ""USD"": 1.183894
	            }
            }";
        private const string JsonGbpRate = 
            @"{
	            ""base"": ""GBP"",
	            ""date"": ""2021-04-07"",
	            ""time_last_updated"": 1617753602,
	            ""rates"": {
		            ""GBP"": 1,
		            ""EUR"": 1.168852,
		            ""USD"": 1.384935
	            }
            }";
        private const string JsonUsdRate = 
            @"{
	            ""base"": ""USD"",
	            ""date"": ""2021-04-07"",
	            ""time_last_updated"": 1617753602,
	            ""rates"": {
		            ""GBP"": 0.722077,
		            ""EUR"": 0.844712,
		            ""USD"": 1
	            }
            }";

        private const string BaseUrl = "https://not-a-real-api.com";

        private readonly IRetrieveExchangeRates _retriever;
        private readonly Mock<HttpMessageHandler> _messageHandler;

        public ExchangeRatesRetrieverTests()
        {
            _messageHandler = new Mock<HttpMessageHandler>();

            var clientFactory = new Mock<IHttpClientFactory>();
            clientFactory
                .Setup(factory => factory.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_messageHandler.Object));
            
            var configuration = new Mock<IHaveConfigurations>();
            configuration
                .Setup(config => config.RatesApiBaseUrl)
                .Returns(BaseUrl);
            
            _retriever = new ExchangeRatesRetriever(configuration.Object, clientFactory.Object);
        }

        [Fact] 
        public async Task returns_an_empty_exchange_rate()
        {
            _messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });
            
            var emptyRate = await _retriever.Get(Currency.EUR);

            Assert.IsType<EmptyExchangeRate>(emptyRate);
        }

        [Fact] 
        public async Task returns_an_exchange_rate()
        {
            _messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        JsonEurRate,
                        Encoding.UTF8,
                        "application/json")
                });
            var expected = JObject.Parse(JsonEurRate);
            
            var eurRate = await _retriever.Get(Currency.EUR);
            
            Assert.Equal(expected["time_last_updated"].Value<long>(), eurRate.UpdatedAt);
            Assert.Equal(expected["rates"]["EUR"].Value<decimal>(), eurRate.Rates[Currency.EUR]);
            Assert.Equal(expected["rates"]["GBP"].Value<decimal>(), eurRate.Rates[Currency.GBP]);
            Assert.Equal(expected["rates"]["USD"].Value<decimal>(), eurRate.Rates[Currency.USD]);
        }

        [Fact]
        public async Task should_throw_when_http_client_fails()
        {
            _messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new OutOfMemoryException());

            await Assert.ThrowsAsync<OutOfMemoryException>(() => _retriever.Get(Currency.EUR));
        }

        [Fact]
        public async Task returns_empty_when_no_currencies_are_found()
        {
            _messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });
            
            var emptyRates = await _retriever.GetAll();

            Assert.Empty(emptyRates);
        }

        [Fact] 
        public async Task returns_almost_all_exchange_rates()
        {
            _messageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        JsonEurRate,
                        Encoding.UTF8,
                        "application/json")
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });
            
            var incompleteRates = await _retriever.GetAll();
            
            Assert.Single(incompleteRates);
            Assert.Equal(Currency.EUR, incompleteRates.First().Key);
            var eurRate = incompleteRates[Currency.EUR];
            
            var expected = JObject.Parse(JsonEurRate);
            Assert.Equal(expected["time_last_updated"].Value<long>(), eurRate.UpdatedAt);
            Assert.Equal(expected["rates"]["EUR"].Value<decimal>(), eurRate.Rates[Currency.EUR]);
            Assert.Equal(expected["rates"]["GBP"].Value<decimal>(), eurRate.Rates[Currency.GBP]);
            Assert.Equal(expected["rates"]["USD"].Value<decimal>(), eurRate.Rates[Currency.USD]);
        }

        [Fact] 
        public async Task returns_all_exchange_rates()
        {
            _messageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        JsonEurRate,
                        Encoding.UTF8,
                        "application/json")
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        JsonGbpRate,
                        Encoding.UTF8,
                        "application/json")
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        JsonUsdRate,
                        Encoding.UTF8,
                        "application/json")
                });
            
            var allRates = await _retriever.GetAll();
            
            Assert.Equal(3, allRates.Count);

            var eurRate = allRates[Currency.EUR];
            var expectedEurRate = JObject.Parse(JsonEurRate);
            Assert.Equal(expectedEurRate["time_last_updated"].Value<long>(), eurRate.UpdatedAt);
            Assert.Equal(expectedEurRate["rates"]["EUR"].Value<decimal>(), eurRate.Rates[Currency.EUR]);
            Assert.Equal(expectedEurRate["rates"]["GBP"].Value<decimal>(), eurRate.Rates[Currency.GBP]);
            Assert.Equal(expectedEurRate["rates"]["USD"].Value<decimal>(), eurRate.Rates[Currency.USD]);
            
            var gbpRate = allRates[Currency.GBP];
            var expectedGbpRate = JObject.Parse(JsonGbpRate);
            Assert.Equal(expectedGbpRate["time_last_updated"].Value<long>(), gbpRate.UpdatedAt);
            Assert.Equal(expectedGbpRate["rates"]["EUR"].Value<decimal>(), gbpRate.Rates[Currency.EUR]);
            Assert.Equal(expectedGbpRate["rates"]["GBP"].Value<decimal>(), gbpRate.Rates[Currency.GBP]);
            Assert.Equal(expectedGbpRate["rates"]["USD"].Value<decimal>(), gbpRate.Rates[Currency.USD]);
            
            var usdRate = allRates[Currency.USD];
            var expectedUsdRate = JObject.Parse(JsonUsdRate);
            Assert.Equal(expectedUsdRate["time_last_updated"].Value<long>(), usdRate.UpdatedAt);
            Assert.Equal(expectedUsdRate["rates"]["EUR"].Value<decimal>(), usdRate.Rates[Currency.EUR]);
            Assert.Equal(expectedUsdRate["rates"]["GBP"].Value<decimal>(), usdRate.Rates[Currency.GBP]);
            Assert.Equal(expectedUsdRate["rates"]["USD"].Value<decimal>(), usdRate.Rates[Currency.USD]);
        }
    }
}