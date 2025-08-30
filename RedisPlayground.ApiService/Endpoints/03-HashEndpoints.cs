using RedisPlayground.ApiService.Models;
using StackExchange.Redis;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis Hash operations
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis Hash endpoints to the application
    /// </summary>
    public static void MapHashEndpoints(this WebApplication app)
    {
        var hashes = app.MapGroup("/hashes")
            .WithTags("3. Redis Hashes")
            .WithOpenApi();

        hashes.MapPost("/{key}/fields", SetHashField)
            .WithName("SetHashField")
            .WithSummary("Set hash field")
            .WithDescription("Sets a field-value pair in a hash")
            .Accepts<HashFieldRequest>("application/json")
            .Produces<OperationResult>(StatusCodes.Status200OK);

        hashes.MapGet("/{key}", GetHash)
            .WithName("GetHash")
            .WithSummary("Get entire hash")
            .WithDescription("Gets all field-value pairs in a hash")
            .Produces<HashResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        hashes.MapGet("/{key}/fields/{field}", GetHashField)
            .WithName("GetHashField")
            .WithSummary("Get hash field")
            .WithDescription("Gets a specific field value from a hash")
            .Produces<StringResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Sets a field-value pair in a hash
    /// </summary>
    private static async Task<IResult> SetHashField(string key, HashFieldRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var success = await db.HashSetAsync(key, request.Field, request.Value);
            return TypedResults.Ok(new OperationResult(true, "Hash field set successfully"));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error setting hash field: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets all field-value pairs in a hash
    /// </summary>
    private static async Task<IResult> GetHash(string key, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var hash = await db.HashGetAllAsync(key);
            if (hash.Length == 0)
                return TypedResults.NotFound();

            var result = hash.ToDictionary(h => h.Name.ToString(), h => h.Value.ToString());
            return TypedResults.Ok(new HashResult(key, result));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting hash: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets a specific field value from a hash
    /// </summary>
    private static async Task<IResult> GetHashField(string key, string field, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var value = await db.HashGetAsync(key, field);
            if (!value.HasValue)
                return TypedResults.NotFound();

            return TypedResults.Ok(new StringResult(field, value!));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting hash field: {ex.Message}"));
        }
    }
}
