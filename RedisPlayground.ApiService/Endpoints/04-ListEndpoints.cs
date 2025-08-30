using RedisPlayground.ApiService.Models;
using StackExchange.Redis;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis List operations
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis List endpoints to the application
    /// </summary>
    public static void MapListEndpoints(this WebApplication app)
    {
        var lists = app.MapGroup("/lists")
            .WithTags("4. Redis Lists")
            .WithOpenApi();

        lists.MapPost("/{key}/push", PushToList)
            .WithName("PushToList")
            .WithSummary("Push to list")
            .WithDescription("Pushes values to the left (LPUSH) or right (RPUSH) of a list")
            .Accepts<ListPushRequest>("application/json")
            .Produces<NumericResult>(StatusCodes.Status200OK);

        lists.MapPost("/{key}/pop", PopFromList)
            .WithName("PopFromList")
            .WithSummary("Pop from list")
            .WithDescription("Pops a value from the left (LPOP) or right (RPOP) of a list")
            .Accepts<ListPopRequest>("application/json")
            .Produces<StringResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        lists.MapGet("/{key}", GetList)
            .WithName("GetList")
            .WithSummary("Get list range")
            .WithDescription("Gets a range of elements from a list (default: all elements)")
            .Produces<ListResult>(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Pushes values to a list
    /// </summary>
    private static async Task<IResult> PushToList(string key, ListPushRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            long result;
            if (request.Direction.ToLowerInvariant() == "left")
            {
                result = await db.ListLeftPushAsync(key, request.Values.Select(v => (RedisValue)v).ToArray());
            }
            else
            {
                result = await db.ListRightPushAsync(key, request.Values.Select(v => (RedisValue)v).ToArray());
            }
            return TypedResults.Ok(new NumericResult(result));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error pushing to list: {ex.Message}"));
        }
    }

    /// <summary>
    /// Pops a value from a list
    /// </summary>
    private static async Task<IResult> PopFromList(string key, ListPopRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            RedisValue value;
            if (request.Direction.ToLowerInvariant() == "left")
            {
                value = await db.ListLeftPopAsync(key);
            }
            else
            {
                value = await db.ListRightPopAsync(key);
            }

            if (!value.HasValue)
                return TypedResults.NotFound();

            return TypedResults.Ok(new StringResult(key, value!));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error popping from list: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets a range of elements from a list
    /// </summary>
    private static async Task<IResult> GetList(string key, IConnectionMultiplexer redis, int start = 0, int stop = -1, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var values = await db.ListRangeAsync(key, start, stop);
            var result = values.Select(v => v.ToString()).ToArray();
            return TypedResults.Ok(new ListResult(key, result));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting list: {ex.Message}"));
        }
    }
}
