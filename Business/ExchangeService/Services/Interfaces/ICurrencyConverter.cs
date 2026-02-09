using Business.ExchangeService.DTOs;
using Business.ExchangeService.Enums;



namespace Business.ExchangeService.Services.Interfaces
{
    public interface ICurrencyConverter
    {
        ConvertResponseDTO Convert(CurrencyName cf, CurrencyName ct, decimal amount, decimal rate);
    }
}
