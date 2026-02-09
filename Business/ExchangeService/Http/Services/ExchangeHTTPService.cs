using Business.ExchangeService.DTOs;
using Business.ExchangeService.Http.Clients;
using Business.ExchangeService.Http.Services.Interfaces;
using System.Net;
using System.Net.Http.Json;




namespace Business.ExchangeService.Http.Services
{
    public class ExchangeHTTPService : IExchangeHTTPService
    {

        private readonly ExchangeHTTPClient _client;

        public ExchangeHTTPService(ExchangeHTTPClient client)
        {
            _client = client;
        }


        public async Task<ExRateResponseDTO> GetExRate(string currencyFROM, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"latest/{currencyFROM}");
            using var response = await _client.SendAsync(request, ct);


            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new HttpRequestException("--> Upstream rate limit was reached (HTTP 429).");

            response.EnsureSuccessStatusCode();


            // if there is an exception in response, header is already loaded, no need to wait until body is loaded:
            var payload = await response.Content.ReadFromJsonAsync<ExRateResponseDTO>(cancellationToken: ct) 
                ?? throw new InvalidOperationException("Upstream response was empty.");

            if (!string.Equals(payload.Result, "success", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Upstream returned error: {payload.ErrorType ?? "unknown"}");


            return payload;
        }
    }
}
