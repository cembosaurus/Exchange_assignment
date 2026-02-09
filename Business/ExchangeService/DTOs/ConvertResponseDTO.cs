namespace Business.ExchangeService.DTOs
{
    public class ConvertResponseDTO
    {
        public decimal Amount { get; set; }
        public string InputCurrency { get; set; } = string.Empty;
        public string OutputCurrency { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}
