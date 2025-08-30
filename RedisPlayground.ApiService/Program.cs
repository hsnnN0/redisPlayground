using RedisPlayground.ApiService.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

// OpenAPI configuration for API exploration
builder.Services.AddOpenApi();

// Configure JSON serializer options for minimal APIs
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.AddRedisDistributedCache("cache");
builder.AddRedisClient("cache");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map all Redis endpoints using extension methods
app.MapRedisEndpoints();

app.MapDefaultEndpoints();

app.Run();
