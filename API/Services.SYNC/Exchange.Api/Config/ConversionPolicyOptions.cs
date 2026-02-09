namespace Exchange.Api.Config
{
    public class ConversionPolicyOptions
    {
        public const string SectionName = "ConversionPolicy";

        public List<string> AllowedInputCurrencies { get; set; } = new();
        public List<string> AllowedOutputCurrencies { get; set; } = new();
    }
}
