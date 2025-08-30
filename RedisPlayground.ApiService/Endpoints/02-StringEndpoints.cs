using Microsoft.AspNetCore.Mvc;
using RedisPlayground.ApiService.Models;
using StackExchange.Redis;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis String operations
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis String endpoints to the application
    /// </summary>
    public static void MapStringEndpoints(this WebApplication app)
    {
        var strings = app.MapGroup("/strings")
            .WithTags("2. Redis Strings")
            .WithOpenApi();

        strings.MapPost("/{key}", SetString)
            .WithName("SetString")
            .WithSummary("Set string value")
            .WithDescription("Sets a string value with optional expiration")
            .Accepts<SetStringRequest>("application/json")
            .Produces<OperationResult>(StatusCodes.Status200OK);

        strings.MapGet("/{key}", GetString)
            .WithName("GetString")
            .WithSummary("Get string value")
            .WithDescription("Gets a string value by key")
            .Produces<StringResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        strings.MapPost("/{key}/increment", IncrementString)
            .WithName("IncrementString")
            .WithSummary("Increment numeric string")
            .WithDescription("Increments a numeric string value")
            .Accepts<IncrementRequest>("application/json")
            .Produces<NumericResult>(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Sets a string value with optional expiration
    /// </summary>
    private static async Task<IResult> SetString(string key, [FromBody] SetStringRequest request, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var expiry = request.ExpirationSeconds.HasValue ? TimeSpan.FromSeconds(request.ExpirationSeconds.Value) : (TimeSpan?)null;
            var success = await db.StringSetAsync(key, request.Value, expiry);
            return TypedResults.Ok(new OperationResult(success, success ? "String set successfully" : "Failed to set string"));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error setting string: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets a string value by key
    /// </summary>
    private static async Task<IResult> GetString(string key, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            if (!value.HasValue)
                return TypedResults.NotFound();

            return TypedResults.Ok(new StringResult(key, value!));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting string: {ex.Message}"));
        }
    }

    /// <summary>
    /// Increments a numeric string value
    /// </summary>
    private static async Task<IResult> IncrementString(string key, [FromBody] IncrementRequest request, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var result = await db.StringIncrementAsync(key, request.Amount);
            return TypedResults.Ok(new NumericResult(result));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error incrementing string: {ex.Message}"));
        }
    }
}
