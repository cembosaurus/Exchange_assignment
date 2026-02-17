using Business.ExchangeService.DTOs;
using Business.ExchangeService.Enums;
using Business.ExchangeService.Services.Interfaces;



namespace Business.ExchangeService.Services
{

    public class CurrencyConverter : ICurrencyConverter
    {

        private const int DefaultDecimalPlaces = 2;


        // method needs only AMOUNT and RATE to calculate the converted value,
        // but I added the input and output currency for better traceability in the response:
        public ConvertResponseDTO Convert(CurrencyName cf, CurrencyName ct, decimal amount, decimal rate)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be zero or positive !");

            if (rate <= 0)
                throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be greater than zero !");


            var rawValue = (cf == ct) ? amount : amount * rate;

            // round the calculated value to 2 decimal places:
            var roundedValue = Math.Round(rawValue, DefaultDecimalPlaces, MidpointRounding.AwayFromZero);


            return new ConvertResponseDTO
            {
                Amount = amount,
                InputCurrency = cf.ToString(),
                OutputCurrency = ct.ToString(),
                Value = roundedValue
            };
        }

    }



}
