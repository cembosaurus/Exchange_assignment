# ExchangeService API (AUD → USD)

A small ASP.NET Core Web API that converts an amount from **AUD** to **USD** using the free endpoint from **ExchangeRate-API** (`open.er-api.com`).

This solution is intentionally minimal but **functionally complete** for the assessment, with emphasis on:

- Maintainability (clear separation of concerns: controller → service → HTTP client)
- Extensibility (conversion policy is config-driven)
- Testability (interfaces for key services, easy to mock)
- Performance under concurrency (in-memory caching + stampede protection + rate limiting)

---

## What the API does

1. **Accepts POST requests** to `/ExchangeService` with:
   - `amount`
   - `inputCurrency`
   - `outputCurrency`

2. **Validates input**
   - Model validation via `[ApiController]` + data annotations on `ConvertRequestDTO`.

3. **Enforces assignment conversion policy**
   - Only currencies allowed by configuration are supported (currently `AUD` as input and `USD` as output).

4. **Fetches exchange rates from upstream**
   - Calls `GET https://open.er-api.com/v6/latest/{BASE}`.

5. **Caches the upstream response**
   - Caches the exchange-rate snapshot (pair rate) in IMemoryCache (keyed by FROM/TO) until time_next_update_unix (best effort).
   - Uses a shared async lock (`SemaphoreSlim`) to avoid request stampede when the cache expires.

6. **Applies API-side rate limiting**
   - Protects your API from abuse and reduces pressure on the upstream service.

---

## How to run

### Prerequisites

- .NET SDK 8 (or newer)

### Run from Visual Studio

1. Set the `Exchange.Api` project as Startup Project.
2. Run (F5).
3. Swagger will open automatically (Development environment).

### Run from CLI

From the solution root:

```bash
dotnet restore
dotnet run --project API/Services.SYNC/Exchange.Api/Exchange.Api.csproj
```

The default ports (from `launchSettings.json`) are:

- HTTP: `http://localhost:2000`
- HTTPS: `https://localhost:2001`

---

## Endpoint

### Request

```bash
curl -X 'POST' \
  'http://localhost:2000/ExchangeService' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '{
    "amount": 5,
    "inputCurrency": "AUD",
    "outputCurrency": "USD"
  }'
```

### Response

```json
{
  "amount": 5,
  "inputCurrency": "AUD",
  "outputCurrency": "USD",
  "value": 3.28
}
```

> `value` depends on the current exchange rate.

---

## Configuration

All settings are in `appsettings.json`.

```json
{
  "ExchangeRateApi": {
    "BaseUrl": "https://open.er-api.com/v6/",
    "TimeoutSeconds": 5
  },
  "ConversionPolicy": {
    "AllowedInputCurrencies": ["AUD"],
    "AllowedOutputCurrencies": ["USD"]
  }
}
```

### Startup config validation (fail fast)

Conversion policy is validated at startup:

- required and non-empty arrays
- no empty strings
- each value must parse into the `CurrencyName` enum

If validation fails, the app logs the failures and stops with `Environment.ExitCode = 1`.

This is intentional: invalid policy config is an **operator error** and should fail fast.

---

## Rate limiting

This project uses **ASP.NET Core built-in rate limiting** (`Microsoft.AspNetCore.RateLimiting` / `System.Threading.RateLimiting`).

- **Policy name:** `exchange_service_rate_limit`
- **Algorithm:** Token Bucket
- **Default settings:**
  - `TokenLimit = 30`
  - `TokensPerPeriod = 30`
  - `ReplenishmentPeriod = 1 minute`
  - `AutoReplenishment = true`
  - `QueueLimit = 0` (requests over the limit are rejected immediately)

Implementation notes:

- Configured via `services.AddRateLimiter(...)` using the policy name above.
- Enabled in the pipeline via `app.UseRateLimiter()`.
- Applied to the endpoint using either `RequireRateLimiting(PolicyName)` or `[EnableRateLimiting(PolicyName)]`.

### What clients see

If a client exceeds the limit, the API responds with:

- **HTTP 429 Too Many Requests**

### Caveat

The built-in limiter is **in-memory**, so in a multi-instance deployment (e.g., Kubernetes) each instance enforces its own limit unless you implement distributed rate limiting.

---

## Error handling

- **400 Bad Request**
  - Invalid request DTO (handled automatically by `[ApiController]`)
  - Unknown currency codes (not in enum)
  - Currency not allowed by configured conversion policy

- **502 Bad Gateway**
  - Upstream returned an unexpected payload (e.g., missing required rate)

- **503 Service Unavailable**
  - Upstream failures (including upstream 429 rate limiting)

- **429 Too Many Requests**
  - API-side rate limiting (client exceeded local quota)

---

## Performance notes (concurrency focus)

### 1) Typed HttpClient + headers-only completion

`ExchangeHTTPClient` uses:

- `HttpCompletionOption.ResponseHeadersRead` so we can fail early on non-success status codes without waiting for the full body.

### 2) Cache + stampede protection

- `IMemoryCache` stores the rate (keyed by pair) until upstream `time_next_update_unix`.
- `SemaphoreSlim` ensures only **one** request refreshes the cache when it expires.
- Other concurrent requests wait briefly and then read from the refreshed cache.

### 3) API-side rate limiting

- Prevents excessive requests from overwhelming your API and reduces the chance of hitting the upstream provider’s rate limit.

---

## Caveats

1. **Upstream dependency**
   - If upstream is down or rate-limited and cache is expired, conversions cannot refresh.

2. **Single conversion policy**
   - Only conversions allowed by configuration are supported (currently AUD → USD).

3. **Rounding**
   - Output is rounded to 2 decimals using `MidpointRounding.AwayFromZero`.

---

## Improvements for production (not implemented)

- **Distributed cache** (Redis) to share rates across instances
- **Cache the full `/latest` exchange-rate snapshot (default base USD)** to prevent frequent upstream refreshes when a requested currency pair is not already cached
- **Resilience policies** (retries/backoff/circuit breaker) using Polly
- **Structured logging** + correlation IDs + observability (metrics/tracing)
- **Consistent error contracts** using RFC7807 `ProblemDetails` for all failures
- **Rate limiting refinements**: partitioning (per client IP / API key), distributed/global enforcement, better 429 payload
- **Health checks** (liveness/readiness + upstream dependency check)
- **Security** (authN/authZ) if required by the hosting environment

---

## Testing (recommended improvement)

To strengthen testability in production, add a small test project with:

### Unit tests (fast)

- `CurrencyConverter`:
  - correct multiplication and rounding
  - guard clauses (negative amount, rate <= 0)
- `ExchangeService` (mock `IExchangeHTTPService`):
  - policy enforcement (AUD→USD)
  - unknown currency rejection
  - cache hit bypasses upstream calls
  - mapping upstream DTO → internal model

### Integration tests (end-to-end)

- Use `WebApplicationFactory<Program>`
- Replace `IExchangeHTTPService` with a fake implementation so tests are deterministic
- Validate:
  - `POST /ExchangeService` returns 200 for valid request
  - `[ApiController]` validation returns 400 for invalid payloads
  - upstream failures map to expected 502/503

---

## Attribution

Exchange rates provided by Exchange Rate API (as required by their terms):
- `https://www.exchangerate-api.com`
