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
            try
            {
                var result = await _exchangeService.ConvertAsync(request, ct);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                // invalid currency codes or policy mismatch:
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // upstream returned unexpected payload / missing rate
                return StatusCode(StatusCodes.Status502BadGateway, ex.Message);
            }
        }


    }

}
