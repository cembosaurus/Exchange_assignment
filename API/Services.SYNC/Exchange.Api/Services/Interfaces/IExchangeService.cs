using Business.ExchangeService.DTOs;
using Business.ExchangeService.Enums;
using Exchange.Api.Models;



namespace Exchange.Api.Services.Interfaces
{
    public interface IExchangeService
    {
        Task<ConvertResponseDTO> ConvertAsync(ConvertRequestDTO requestDTO, CancellationToken ct);
    }
}
