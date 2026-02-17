using Business.ExchangeService.DTOs;
using Exchange.Api.Config;
using Exchange.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;



namespace Exchange.Api.Controllers
{

    [ApiController]
    [Route("ExchangeService")]
    [EnableRateLimiting(RateLimitingModule.PolicyName)]
    public class CurrencyConversionController : ControllerBase
    {

        private readonly IExchangeService _exchangeService;


        public CurrencyConversionController(IExchangeService exchangeService)
        {
            _exchangeService = exchangeService;
        }





        [HttpPost]
        [ProducesResponseType(typeof(ConvertResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<ConvertResponseDTO>> Convert([FromBody] ConvertRequestDTO request, CancellationToken ct)
        {
            var result = await _exchangeService.ConvertAsync(request, ct);

            return Ok(result);
        }


    }

}
