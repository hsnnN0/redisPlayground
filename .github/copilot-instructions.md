## Copilot instructions for RedisPlayground

Purpose: give an AI coding agent the minimum, high-value knowledge to be productive in this repo.

- Big picture
  - Solution layout: `RedisPlayground.sln` with four primary projects:
    - `RedisPlayground.Web` (Blazor Server UI + HTTP clients)
    - `RedisPlayground.ApiService` (backend API service)
    - `RedisPlayground.AppHost` (host/process helpers)
    - `RedisPlayground.ServiceDefaults` (common DI, service discovery, health, OpenTelemetry)
  - High-level flow: UI calls services via typed `HttpClient` (example: `WeatherApiClient` in `RedisPlayground.Web`) using the service-discovery scheme `https+http://{servicename}`; shared defaults (resilience, discovery, health checks, OTel) are wired in `ServiceDefaults/Extensions.cs`.

- Key patterns & where to look (concrete examples)
  - Service defaults: `RedisPlayground.ServiceDefaults/Extensions.cs` — read this first if changing cross-cutting behavior (logging, OTel, health checks, HTTP defaults, service discovery).
  - App startup: `RedisPlayground.Web/Program.cs` — shows how defaults are applied and features enabled:
    - `builder.AddServiceDefaults();`
    - `builder.AddRedisOutputCache("cache");` (registers Redis-backed output caching)
    - `builder.Services.AddHttpClient<WeatherApiClient>(...)` with `client.BaseAddress = new("https+http://apiservice")` — service discovery scheme in use.
  - Typed HTTP client example: `RedisPlayground.Web/WeatherApiClient.cs` consumes `/weatherforecast` via `GetFromJsonAsAsyncEnumerable`.

- Redis & caching specifics
  - Redis output cache is enabled in `Program.cs` via `AddRedisOutputCache("cache")` and `app.UseOutputCache()` — look here when troubleshooting cache behavior.
  - NuGet packages: `Aspire.StackExchange.Redis.OutputCaching` is referenced in `RedisPlayground.Web.csproj`; the repo also pulls `StackExchange.Redis` through dependencies (see `obj/project.assets.json`).
  - If you need to change connection settings, search appsettings files (`appsettings.json`, `appsettings.Development.json`) and `Program.cs` for where the cache/redis options are bound.

- Service discovery and HTTP conventions
  - `ServiceDefaults/Extensions.cs` configures service discovery by calling `builder.Services.AddServiceDiscovery()` and `http.AddServiceDiscovery()` for HttpClient defaults. The `https+http://` scheme (example: `https+http://apiservice`) means "prefer HTTPS, fallback to HTTP" and is resolved by the service-discovery integration.

- Observability & health
  - OpenTelemetry: instrumentation is enabled in `Extensions.cs`. The OTLP exporter is toggled by the `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable.
  - Health endpoints: `MapDefaultEndpoints()` in `Extensions.cs` maps `/health` and `/alive` but only when `app.Environment.IsDevelopment()`; note the security comment — health endpoints are development-only by default here.

- Build / run / debug (concrete commands)
  - Build entire solution: `dotnet build RedisPlayground.sln`
  - Run Web project locally: `dotnet run --project RedisPlayground.Web` (or use IDE launch via `Properties/launchSettings.json`).
  - To run ApiService: `dotnet run --project RedisPlayground.ApiService`.

- Conventions and gotchas for AI agents
  - Read `RedisPlayground.ServiceDefaults/Extensions.cs` before altering startup middleware or HttpClient behavior — many services rely on these defaults.
  - Prefer editing `Program.cs` in the specific project (Web or ApiService) when enabling or disabling middleware (e.g., output cache, antiforgery, HSTS).
  - When adding new HTTP calls, follow the typed-HttpClient pattern (register client in `Program.cs` and add a small wrapper class like `WeatherApiClient`).
  - No unit tests detected in the repo root; if you add tests, place them in a conventional `*.Tests` project and wire them into solution.

- Where to find configuration
  - Project-level settings: `*/appsettings.json` and `*/appsettings.Development.json` (each project has its own copy).
  - Sensitive secrets: not present in source—use environment variables or secrets manager as expected by the service defaults.

- Quick checklist for PRs an AI agent might open
  - Did you run `dotnet build` for the solution? (required)
  - If you changed startup behavior, did you update `ServiceDefaults/Extensions.cs` or the specific project's `Program.cs` consistently?
  - For Redis/caching changes, confirm `AddRedisOutputCache` usage and any appsettings keys you added are present and documented.

If anything here is unclear or you want the file to include more examples (small snippets, common appsettings keys, or sample Redis connection values for local development), tell me what to expand and I'll iterate.
