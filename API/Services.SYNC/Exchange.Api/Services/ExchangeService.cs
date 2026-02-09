using Business.ExchangeService.DTOs;
using Business.ExchangeService.Enums;
using Business.ExchangeService.Http.Services.Interfaces;
using Business.ExchangeService.Services.Interfaces;
using Exchange.Api.Config;
using Exchange.Api.Models;
using Exchange.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;



namespace Exchange.Api.Services
{
    public class ExchangeService : IExchangeService
    {

        private readonly IExchangeHTTPService _httpService;
        private readonly ICurrencyConverter _converter;
        private readonly IMemoryCache _cache;
        private const string CacheKeyFormat = "exrate_snapshot_{0}_{1}_v1";

        private readonly HashSet<CurrencyName> _allowedInputs;
        private readonly HashSet<CurrencyName> _allowedOutputs;

        // static because all requests should share one lock,
        // to ensure only one refresh call if cache expires:
        private static readonly SemaphoreSlim _refreshLock = new(1, 1); 


        public ExchangeService(IExchangeHTTPService httpService, ICurrencyConverter converter, IMemoryCache cache, IOptions<ConversionPolicyOptions> policyOptions)
        {
            _httpService = httpService;
            _converter = converter;
            _cache = cache;

            var policy = policyOptions.Value;

            _allowedInputs = policy.AllowedInputCurrencies
                .Select(x => Enum.Parse<CurrencyName>(x.Trim(), ignoreCase: false))
                .ToHashSet();

            _allowedOutputs = policy.AllowedOutputCurrencies
                .Select(x => Enum.Parse<CurrencyName>(x.Trim(), ignoreCase: false))
                .ToHashSet();
        }





        public async Task<ConvertResponseDTO> ConvertAsync(ConvertRequestDTO requestDTO, CancellationToken ct)
        {
            var invalid = new List<string>(capacity: 2);

            if (!TryParseCurrencyName(requestDTO.InputCurrency, out var currencyFROM))
                invalid.Add(requestDTO.InputCurrency);

            if (!TryParseCurrencyName(requestDTO.OutputCurrency, out var currencyTO))
                invalid.Add(requestDTO.OutputCurrency);

            if (invalid.Count > 0)
                throw new ArgumentException(BuildUnknownCurrencyMessage(invalid));

            if (!_allowedInputs.Contains(currencyFROM))
                throw new ArgumentException($"Input currency {currencyFROM} is not allowed by policy.");

            if (!_allowedOutputs.Contains(currencyTO))
                throw new ArgumentException($"Output currency {currencyTO} is not allowed by policy.");


            var rateModel = await GetRateAsync(currencyFROM, currencyTO, ct);

            var conversionResult = _converter.Convert(currencyFROM, currencyTO, requestDTO.Amount, rateModel.Rate);


            return new ConvertResponseDTO
            {
                Amount = requestDTO.Amount,
                InputCurrency = currencyFROM.ToString(),
                OutputCurrency = currencyTO.ToString(),
                Value = conversionResult.Value
            };
        }



        private async Task<ExRate> GetRateAsync(CurrencyName currencyFROM, CurrencyName currencyTO, CancellationToken ct)
        {
            var cacheKey = string.Format(CacheKeyFormat, currencyFROM, currencyTO);

            return await StoreRateInCacheAsync(
                    cacheKey,
                    async () =>
                    {
                        var rateDTO = await _httpService.GetExRate(currencyFROM.ToString(), ct);

                        var rateModel = MapDTOtoModel(rateDTO, currencyTO);

                        // sets the cache lifetime based on "next update" in response:
                        var expiresAt = rateModel.TimeNextUpdateUnix > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(rateModel.TimeNextUpdateUnix)
                            : DateTimeOffset.UtcNow.AddHours(1);    // if update time is unusable then set refresh time (http call) to 1 hour

                        return (rateModel, expiresAt);
                    },
                    ct
                );
        }



        private async Task<T> StoreRateInCacheAsync<T>(string cacheKey, Func<Task<(T Value, DateTimeOffset ExpiresAt)>> factory, CancellationToken ct) where T : notnull
        {
            // if rate is NOT expired in cache,
            // then get rate from cache and bypass http 'refresh rate' call,
            // for better performance:
            if (_cache.TryGetValue(cacheKey, out T? cached) && cached is not null)
                return cached!;

            await _refreshLock.WaitAsync(ct);
            try
            {
                // check again when multiple requests are locked in, and each of them is
                // trying to make http call to refresh rate after first request did it already:
                if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
                    return cached!;

                var (rateModel, expiresAt) = await factory();

                _cache.Set(cacheKey, rateModel, expiresAt);
                return rateModel;
            }
            finally
            {
                _refreshLock.Release();
            }
        }




        private static ExRate MapDTOtoModel(ExRateResponseDTO rateDTO, CurrencyName ct)
        {
            var currencyTO = ct.ToString();

            if (rateDTO.Rates is null || !rateDTO.Rates.TryGetValue(currencyTO, out var rate))
                throw new InvalidOperationException($"{currencyTO} rate is missing in upstream response.");

            return new ExRate
            {
                CurrencyFrom = rateDTO.BaseCode ?? string.Empty,
                CurrencyTo = currencyTO,
                Rate = rate,
                TimeNextUpdateUnix = rateDTO.TimeNextUpdateUnix ?? 0
            };
        }



        private static bool TryParseCurrencyName(string? currencyString, out CurrencyName currencyEnum)
        {
            currencyString = (currencyString ?? string.Empty).Trim();

            return Enum.TryParse(currencyString, ignoreCase: false, out currencyEnum);
        }



        private static string BuildUnknownCurrencyMessage(IReadOnlyList<string> invalidRawCodes)
        {
            static string Clean(string? s) => string.IsNullOrWhiteSpace(s) ? "<empty>" : s.Trim();

            if (invalidRawCodes.Count == 1)
                return $"Currency {Clean(invalidRawCodes[0])} doesn't match any data in our database.";

            if (invalidRawCodes.Count == 2)
                return $"Currencies {Clean(invalidRawCodes[0])} and {Clean(invalidRawCodes[1])} don't match any data in our database.";

            var joined = string.Join(", ", invalidRawCodes.Select(Clean));

            return $"Currencies {joined} don't match any data in our database.";
        }


    }
}
