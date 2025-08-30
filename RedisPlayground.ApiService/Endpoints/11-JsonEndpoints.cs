using RedisPlayground.ApiService.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis JSON operations (simulated)
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis JSON endpoints to the application
    /// </summary>
    public static void MapJsonEndpoints(this WebApplication app)
    {
        var json = app.MapGroup("/json")
            .WithTags("11. Redis JSON")
            .WithOpenApi();

        json.MapPost("/{key}", SetJson)
            .WithName("SetJson")
            .WithSummary("Set JSON document")
            .WithDescription("Sets a JSON document at the given path")
            .Accepts<JsonSetRequest>("application/json")
            .Produces<OperationResult>(StatusCodes.Status200OK);

        json.MapGet("/{key}", GetJson)
            .WithName("GetJson")
            .WithSummary("Get JSON document")
            .WithDescription("Gets a JSON document or part of it using JSONPath")
            .Produces<JsonResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        json.MapPost("/{key}/path", GetJsonPath)
            .WithName("GetJsonPath")
            .WithSummary("Get JSON by path")
            .WithDescription("Gets part of a JSON document using JSONPath")
            .Accepts<JsonPathRequest>("application/json")
            .Produces<JsonResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        json.MapPut("/{key}/path", SetJsonPath)
            .WithName("SetJsonPath")
            .WithSummary("Set JSON by path")
            .WithDescription("Sets part of a JSON document using JSONPath")
            .Accepts<JsonPathSetRequest>("application/json")
            .Produces<OperationResult>(StatusCodes.Status200OK);

        json.MapDelete("/{key}/path", DeleteJsonPath)
            .WithName("DeleteJsonPath")
            .WithSummary("Delete JSON by path")
            .WithDescription("Deletes part of a JSON document using JSONPath")
            .Accepts<JsonPathRequest>("application/json")
            .Produces<NumericResult>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> SetJson(string key, JsonSetRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            // Note: Basic JSON simulation using String operations since RedisJSON module may not be available
            // In production, you would use RedisJSON commands like JSON.SET
            var jsonString = JsonSerializer.Serialize(request.Value);
            var success = await db.StringSetAsync($"json:{key}", jsonString);
            return TypedResults.Ok(new OperationResult(success, success ? "JSON document set successfully" : "Failed to set JSON document"));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error setting JSON: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetJson(string key, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync($"json:{key}");
            if (!value.HasValue)
                return TypedResults.NotFound();

            return TypedResults.Ok(new JsonResult(key, value!));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting JSON: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetJsonPath(string key, JsonPathRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync($"json:{key}");
            if (!value.HasValue)
                return TypedResults.NotFound();

            // This is a simulation - in production you'd use RedisJSON's JSONPath support
            return TypedResults.Ok(new JsonResult(key, value!));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting JSON path: {ex.Message}"));
        }
    }

    private static async Task<IResult> SetJsonPath(string key, JsonPathSetRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            // This is a simulation - in production you'd use RedisJSON's JSONPath support
            var jsonString = JsonSerializer.Serialize(request.Value);
            var success = await db.StringSetAsync($"json:{key}:{request.Path}", jsonString);
            return TypedResults.Ok(new OperationResult(success, success ? "JSON path set successfully" : "Failed to set JSON path"));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error setting JSON path: {ex.Message}"));
        }
    }

    private static async Task<IResult> DeleteJsonPath(string key, JsonPathRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            // This is a simulation - in production you'd use RedisJSON's JSONPath support
            var deleted = await db.KeyDeleteAsync($"json:{key}:{request.Path}");
            return TypedResults.Ok(new NumericResult(deleted ? 1 : 0));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error deleting JSON path: {ex.Message}"));
        }
    }
}
