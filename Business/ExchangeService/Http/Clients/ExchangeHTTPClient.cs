namespace Business.ExchangeService.Http.Clients
{
    public class ExchangeHTTPClient
    {

        private readonly HttpClient _client;



        public ExchangeHTTPClient(HttpClient client)
        {
            _client = client;
        }




        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default)
        {
            // ResponseHeadersRead - read the header while body is still being loaded,
            // to stop loading the body if header contains exception,
            // good for performance:
            return _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }

    }
}
