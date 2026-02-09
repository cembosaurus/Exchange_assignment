using System.Threading.RateLimiting;



namespace Exchange.Api.Config
{
    public static class RateLimitingModule
    {

        public const string PolicyName = "exchange_service_rate_limit";


        public static IServiceCollection AddExchangeRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(PolicyName, httpContext =>
                {
                    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetTokenBucketLimiter(ip, _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 30,
                        TokensPerPeriod = 30,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });

                });

            });


            return services;
        }



        public static IApplicationBuilder UseExchangeRateLimiting(this IApplicationBuilder app)
        {
            app.UseRateLimiter();

            return app;
        }


    }
}
