namespace Exchange.Api.Middlewares
{
    public sealed class ExceptionHandlerMW
    {

        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMW> _logger;
        private readonly IHostEnvironment _env;


        public ExceptionHandlerMW(RequestDelegate next, ILogger<ExceptionHandlerMW> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }




        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // if client closes browser or loses connection during the processing the request no need to log it as error.
                // f.e: when client is set up with HttpCompletionOption.ResponseHeadersRead and disconnects before reading the response body,
                // it will trigger this exception.
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning(ex, "Response already started; cannot write error response.");

                    throw;
                }


                var (status, title, type) = Map(ex);

                // 400 as warning, and 500 as error:
                if (status >= 500)
                    _logger.LogError(ex, "Unhandled exception mapped to {StatusCode}", status);
                else
                    _logger.LogWarning(ex, "Request failed mapped to {StatusCode}", status);

                var detail = ShouldExposeDetails(status) ? ex.Message : "An unexpected error occurred.";

                context.Response.Clear();

                await Results.Problem(
                    title: title,
                    detail: detail,
                    statusCode: status,
                    type: type,
                    instance: context.Request.Path
                ).ExecuteAsync(context);
            }

        }



        private bool ShouldExposeDetails(int statusCode)
        {
            // expose 500 details only in development:
            return statusCode < 500 || _env.IsDevelopment();
        }



        private static (int Status, string Title, string Type) Map(Exception ex)
        {
            return ex switch
            {
                ArgumentException =>
                    (StatusCodes.Status400BadRequest,
                     "Bad request",
                     "https://tools.ietf.org/html/rfc9110#section-15.5.1"),

                InvalidOperationException =>
                    (StatusCodes.Status502BadGateway,
                     "Bad gateway",
                     "https://tools.ietf.org/html/rfc9110#section-15.6.3"),

                HttpRequestException =>
                    (StatusCodes.Status503ServiceUnavailable,
                     "Service unavailable",
                     "https://tools.ietf.org/html/rfc9110#section-15.6.4"),

                _ =>
                    (StatusCodes.Status500InternalServerError,
                     "Internal server error",
                     "https://tools.ietf.org/html/rfc9110#section-15.6.1")
            };
        }

    }
}
