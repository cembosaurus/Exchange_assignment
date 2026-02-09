using Business.ExchangeService.Enums;
using Microsoft.Extensions.Options;



namespace Exchange.Api.Config
{
    public static class ConfigValidationModule
    {
        public static IServiceCollection AddConversionPolicyValidation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<ConversionPolicyOptions>()
            .Bind(configuration.GetSection(ConversionPolicyOptions.SectionName))           

            // required & non empty array
            .Validate(cpo => cpo.AllowedInputCurrencies != null && cpo.AllowedInputCurrencies.Count > 0,
                "  ConversionPolicy: AllowedInputCurrencies must contain at least one value.")
            .Validate(cpo => cpo.AllowedOutputCurrencies != null && cpo.AllowedOutputCurrencies.Count > 0,
                "  ConversionPolicy: AllowedOutputCurrencies must contain at least one value.")

            // no empty strings
            .Validate(cpo => cpo.AllowedInputCurrencies != null && cpo.AllowedInputCurrencies.All(x => !string.IsNullOrWhiteSpace(x)),
                "  ConversionPolicy: AllowedInputCurrencies contains empty value.")
            .Validate(cpo => cpo.AllowedOutputCurrencies != null && cpo.AllowedOutputCurrencies.All(x => !string.IsNullOrWhiteSpace(x)),
                "  ConversionPolicy: AllowedOutputCurrencies contains empty value.")

            // validate against enum
            .Validate(cpo => cpo.AllowedInputCurrencies != null && cpo.AllowedInputCurrencies.All(x =>
                    Enum.TryParse<CurrencyName>(x.Trim(), ignoreCase: false, out _)),
                "  ConversionPolicy: AllowedInputCurrencies must match supported currency enum values.")
            .Validate(cpo => cpo.AllowedOutputCurrencies != null && cpo.AllowedOutputCurrencies.All(x =>
                    Enum.TryParse<CurrencyName>(x.Trim(), ignoreCase: false, out _)),
                "  ConversionPolicy: AllowedOutputCurrencies must match supported currency enum values.");

            return services;
        }


        public static bool ValidateConversionPolicyAtStartup(WebApplication app)
        {
            try
            {
                _ = app.Services.GetRequiredService<IOptions<ConversionPolicyOptions>>().Value;

                return true;
            }
            catch (OptionsValidationException ex)
            {
                Console.Error.WriteLine("--> Invalid Conversion Policy configuration:");

                foreach (var failure in ex.Failures)
                    Console.Error.WriteLine($"- {failure}");

                return false;
            }
        }

    }
}
