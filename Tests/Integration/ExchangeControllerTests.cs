using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using API;
using API.Helpers;
using API.Models;
using Main;
using Main.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Tests.Integration
{
    public class ExchangeControllerTests
    {
        private readonly HttpClient _client;
        private readonly Mock<IRetrieveExchangeRates> _retriever;
        private readonly Mock<ICalculateExchangeRates> _calculator;
        private readonly Mock<IBuildExchangeRequests> _requestBuilder;

        public ExchangeControllerTests()
        {
            _requestBuilder = new Mock<IBuildExchangeRequests>();
            _calculator = new Mock<ICalculateExchangeRates>();
            _retriever = new Mock<IRetrieveExchangeRates>();
            
            var webHostBuilder = new WebHostBuilder();
            webHostBuilder
                .ConfigureTestServices(services =>
                {
                    services.AddTransient(_ => _requestBuilder.Object);
                    services.AddTransient(_ => _calculator.Object);
                })
                .UseStartup<Startup>();

            var testServer = new TestServer(webHostBuilder);
            _client = testServer.CreateClient();
        }

        [Fact]
        public async Task returns_404_when_currency_not_found()
        {
            _requestBuilder
                .Setup(builder => builder.Build(It.IsAny<RequestInfo>()))
                .Returns(new BuildResult(string.Empty, new ExchangeRequest()));
            _calculator
                .Setup(calculator => calculator.Exchange(It.IsAny<ExchangeRequest>()))
                .ReturnsAsync(new EmptyExchangeResponse());
            var jsonContent = new StringContent(@"{""source"": ""EUR""}", Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/exchange", jsonContent);
            
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            
            var jsonString = await response.Content.ReadAsStringAsync();
            var errorMessage = JObject.Parse(jsonString);
            Assert.Equal(
                $"The exchange information for the currency EUR was not found or its out of date",
                errorMessage["errorMessage"].Value<string>());
        }

        [Fact]
        public async Task returns_503_when_an_exception_is_caught()
        {
            _requestBuilder
                .Setup(builder => builder.Build(It.IsAny<RequestInfo>()))
                .Throws<OutOfMemoryException>();
            var jsonContent = new StringContent(@"{""source"": ""EUR""}", Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/exchange", jsonContent);
            
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        [Fact]
        public async Task returns_200_when_price_has_been_exchanged()
        {
            var localClient = ArrangeClient();
            ArrangeRetriever();
            var jsonContent = new StringContent(
                @"{""price"": 23.32, ""source"": ""EUR"", ""target"":""GBP""}", 
                Encoding.UTF8,
                "application/json");
            
            var response = await localClient.PostAsync("/exchange", jsonContent);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var jsonString = await response.Content.ReadAsStringAsync();
            var exchangeResult = JObject.Parse(jsonString);

            Assert.Equal(
                Currency.GBP,
                Enum.Parse(
                    typeof(Currency),
                    exchangeResult["exchangeIn"].Value<string>()));
            Assert.Equal(19.95m, exchangeResult["exchange"]);
            Assert.Equal(0.855552m, exchangeResult["exchangeRate"]);
        }

        private HttpClient ArrangeClient()
        {
            var webHostBuilder = new WebHostBuilder();
            webHostBuilder
                .ConfigureTestServices(services =>
                {
                    services.AddSingleton(_ => _retriever.Object);
                })
                .UseStartup<Startup>();

            var testServer = new TestServer(webHostBuilder);
            return testServer.CreateClient();
        }

        private void ArrangeRetriever()
        {
            var eurExchangeRate = new Dictionary<Currency, decimal>
            {
                {Currency.EUR, 1m},
                {Currency.GBP, 0.855552m},
                {Currency.USD, 1.183894m}
            };
            
            _retriever
                .Setup(retriever => retriever.GetAll())
                .ReturnsAsync(new Dictionary<Currency, IAmExchangeRate>
                {
                    {Currency.EUR, new ExchangeRate(eurExchangeRate, DateTimeOffset.UtcNow.ToUnixTimeSeconds())}
                });
        }
    }
}