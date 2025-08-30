using RedisPlayground.ApiService.Models;
using StackExchange.Redis;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis Stream operations
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis Stream endpoints to the application
    /// </summary>
    public static void MapStreamEndpoints(this WebApplication app)
    {
        var streams = app.MapGroup("/streams")
            .WithTags("8. Redis Streams")
            .WithOpenApi();

        streams.MapPost("/{stream}/add", AddToStream)
            .WithName("AddToStream")
            .WithSummary("Add to stream")
            .WithDescription("Adds an entry to a Redis stream")
            .Accepts<StreamAddRequest>("application/json")
            .Produces<StreamIdResult>(StatusCodes.Status200OK);

        streams.MapGet("/{stream}", ReadStream)
            .WithName("ReadStream")
            .WithSummary("Read from stream")
            .WithDescription("Reads entries from a Redis stream")
            .Produces<StreamResult>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> AddToStream(string stream, StreamAddRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var fields = request.Fields.Select(kvp => new NameValueEntry(kvp.Key, kvp.Value)).ToArray();
            var id = await db.StreamAddAsync(stream, fields);
            return TypedResults.Ok(new StreamIdResult(id!));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error adding to stream: {ex.Message}"));
        }
    }

    private static async Task<IResult> ReadStream(string stream, IConnectionMultiplexer redis, string startId = "0", int count = 10, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var messages = await db.StreamRangeAsync(stream, startId, count: count);
            var entries = messages.Select(m => new Models.StreamEntry(m.Id!, m.Values.ToDictionary(v => v.Name.ToString(), v => v.Value.ToString()))).ToArray();
            return TypedResults.Ok(new StreamResult(stream, entries));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error reading stream: {ex.Message}"));
        }
    }
}
