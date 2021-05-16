using System;
using System.Threading.Tasks;
using API.Helpers;
using API.Models;
using Main;
using Main.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExchangeController: ControllerBase
    {
        private readonly IBuildExchangeRequests _exchangeRequestBuilder;
        private readonly ICalculateExchangeRates _calculator;

        public ExchangeController(IBuildExchangeRequests exchangeRequestBuilder, ICalculateExchangeRates calculator)
        {
            _exchangeRequestBuilder = exchangeRequestBuilder;
            _calculator = calculator;
        }
        
        [HttpPost]
        public async Task<IActionResult> Get(RequestInfo requestInfo)
        {
            IAmExchangeResponse exchange;
            try
            {
                var buildResult = _exchangeRequestBuilder.Build(requestInfo);

                if (buildResult.Error != string.Empty)
                    return BadRequest(new {errorMessage = buildResult.Error});

                exchange = await _calculator.Exchange(buildResult.ExchangeRequest);
                if (exchange is EmptyExchangeResponse)
                {
                    return NotFound(
                        new {errorMessage = 
                            $"The exchange information for the currency {requestInfo.Source} was not found or its out of date"});
                }
            }
            catch (Exception exception)
            {
                // if we move on from the MVP, add a log here with enough details to be able to do a root cause analysis
                return StatusCode(503, new {erroMessage = exception.Message});
            }
            
            return Ok(exchange);
        }
    }
}