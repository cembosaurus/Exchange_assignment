using Business.ExchangeService.Http.Clients;
using Business.ExchangeService.Http.Services;
using Business.ExchangeService.Http.Services.Interfaces;
using Business.ExchangeService.Services;
using Business.ExchangeService.Services.Interfaces;
using Exchange.Api.Config;
using Exchange.Api.Services;
using Exchange.Api.Services.Interfaces;
using System.Net.Http.Headers;




var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IExchangeService, ExchangeService>();
builder.Services.AddScoped<IExchangeHTTPService, ExchangeHTTPService>();
builder.Services.AddSingleton<ICurrencyConverter, CurrencyConverter>(); // stateless singleton - not thread unsafe

builder.Services.Configure<ExchangeRateApiOptions>(builder.Configuration.GetSection(ExchangeRateApiOptions.SectionName));

builder.Services.AddHttpClient<ExchangeHTTPClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExchangeRateApiOptions>>().Value;

    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});


// ASSIGNMENT: enforcing specific conversion policy (right now AUD -> USD):
builder.Services.AddConversionPolicyValidation(builder.Configuration);

// per IP rate limiting:
builder.Services.AddExchangeRateLimiting();



var app = builder.Build();



app.UseExchangeRateLimiting();


// validation of conversion policy - fast fail at startup:
if (!ConfigValidationModule.ValidateConversionPolicyAtStartup(app))
{
    Environment.ExitCode = 1;

    return;
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.MapControllers();



app.Run();

