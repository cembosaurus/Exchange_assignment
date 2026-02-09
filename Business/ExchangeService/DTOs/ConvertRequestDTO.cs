using System.ComponentModel.DataAnnotations;

namespace Business.ExchangeService.DTOs
{
    public class ConvertRequestDTO
    {
        [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "Amount must be between 0 and 1,000,000,000.")]
        public decimal Amount { get; set; }

        [Required]
        [RegularExpression("^[A-Z]{3}$", ErrorMessage = "InputCurrency must be exactly 3 uppercase letters (A–Z), e.g. AUD.")]
        public string InputCurrency { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^[A-Z]{3}$", ErrorMessage = "OutputCurrency must be exactly 3 uppercase letters (A–Z), e.g. USD.")]
        public string OutputCurrency { get; set; } = string.Empty;
    }
}
