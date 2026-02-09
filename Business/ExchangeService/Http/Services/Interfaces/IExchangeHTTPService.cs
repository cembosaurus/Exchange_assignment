using Business.ExchangeService.DTOs;

namespace Business.ExchangeService.Http.Services.Interfaces
{
    public interface IExchangeHTTPService
    {
        Task<ExRateResponseDTO> GetExRate(string currencyFROM, CancellationToken ct = default);
    }
}
