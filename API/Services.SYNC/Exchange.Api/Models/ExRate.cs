using System.Text.Json.Serialization;



namespace Exchange.Api.Models
{

    public class ExRate
    {
        public string CurrencyFrom { get; set; } = string.Empty;
        public string CurrencyTo { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public long TimeNextUpdateUnix { get; set; }
    }

}
