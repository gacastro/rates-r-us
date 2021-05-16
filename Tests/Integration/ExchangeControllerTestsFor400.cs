using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Tests.Integration
{
    public class ExchangeControllerTestsFor400
    {
        private readonly HttpClient _client;

        public ExchangeControllerTestsFor400()
        {
            var webHostBuilder = new WebHostBuilder();
            webHostBuilder.UseStartup<Startup>();

            var testServer = new TestServer(webHostBuilder);
            _client = testServer.CreateClient();
        }

        [Theory]
        [InlineData("not json at all")]
        [InlineData("\"source\": \"eur\", \"target\": \"gbp\"")]
        [InlineData("{source: \"eur\", target: \"gbp\"}")]
        public async Task returns_400_for_invalid_json(string jsonToSend)
        {
            var stringContent = new StringContent(jsonToSend, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/exchange", stringContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("{\"source\": \"eur\", \"target\": \"gbp\"}", false)]
        [InlineData("{\"price\": \"invalid-price\", \"source\": \"eur\", \"target\": \"gbp\"}", true)]
        [InlineData("{\"price\": -23.32, \"source\": \"eur\", \"target\": \"gbp\"}", false)]
        [InlineData("{\"price\": 0, \"source\": \"eur\", \"target\": \"gbp\"}", false)]
        [InlineData("{\"price\": [], \"source\": \"eur\", \"target\": \"gbp\"}", true)]
        [InlineData("{\"price\": {}, \"source\": \"eur\", \"target\": \"gbp\"}", true)]
        [InlineData("{\"price\": null, \"source\": \"eur\", \"target\": \"gbp\"}", true)]
        public async Task returns_400_when_price_is_wrong(string jsonToSend, bool modelBindingError)
        {
            var stringContent = new StringContent(jsonToSend, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/exchange", stringContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            if (!modelBindingError)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var errorMessage = JObject.Parse(jsonString);
            
                Assert.Equal("The price is missing or its not a value greater than 0",
                    errorMessage["errorMessage"].Value<string>());
            }
        }

        [Theory]
        [InlineData("{\"price\": 23.32, \"target\": \"gbp\"}", "Source")]
        [InlineData("{\"price\": 23.32, \"source\": \"eur\"}", "Target")]
        [InlineData("{\"price\": 23.32, \"source\": null, \"target\": null}", "Source,Target")]
        [InlineData("{\"price\": 23.32}", "Source,Target")]
        public async Task returns_400_when_missing_source_or_target(string jsonToSend, string expectedError)
        {
            var stringContent = new StringContent(jsonToSend, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/exchange", stringContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            
            var jsonString = await response.Content.ReadAsStringAsync();
            var errorMessage = JObject.Parse(jsonString);
            Assert.Equal(
                $"The following currencies are missing from the input: '{expectedError}'.",
                errorMessage["errorMessage"].Value<string>());
        }

        [Theory]
        [InlineData("{\"source\": 23.32, \"price\": 23.32, \"target\": \"gbp\"}", "Source", true)]
        [InlineData("{\"source\": \"invalid-source\", \"price\": 23.32, \"target\": \"gbp\"}", "Source", false)]
        [InlineData("{\"source\": \"invalid-source\", \"price\": 23.32, \"target\": \"invalid-source\"}", "Source,Target", false)]
        [InlineData("{\"source\": [], \"price\": 23.32, \"target\": \"gbp\"}", "Source", true)]
        [InlineData("{\"source\": {}, \"price\": 23.32, \"target\": \"gbp\"}", "Source", true)]
        [InlineData("{\"target\": 23.32, \"price\": 23.32, \"source\": \"gbp\"}", "Target", true)]
        [InlineData("{\"target\": \"invalid-source\", \"price\": 23.32, \"source\": \"gbp\"}", "Target", false)]
        [InlineData("{\"target\": [], \"price\": 23.32, \"source\": \"gbp\"}", "Target", true)]
        [InlineData("{\"target\": {}, \"price\": 23.32, \"source\": \"gbp\"}", "Target", true)]
        public async Task returns_400_when_invalid_source_or_target_currency(string jsonToSend, string expectedError, bool modelBindingError)
        {
            var stringContent = new StringContent(jsonToSend, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/exchange", stringContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            if (!modelBindingError)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var errorMessage = JObject.Parse(jsonString);
                Assert.Equal(
                    $"The following currencies are invalid: '{expectedError}'. " +
                    "Please remember that the source or target currency has to be one of: eur, gbp, usd",
                    errorMessage["errorMessage"].Value<string>());
            }
        }
    }
}