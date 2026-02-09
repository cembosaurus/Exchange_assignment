namespace Exchange.Api.Config
{
    public class ExchangeRateApiOptions
    {
        public const string SectionName = "ExchangeRateApi";

        public string BaseUrl { get; set; } = null!;
        public int TimeoutSeconds { get; set; }
    }
}
