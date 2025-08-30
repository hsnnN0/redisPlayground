using Microsoft.AspNetCore.Mvc;
using RedisPlayground.ApiService.Models;
using StackExchange.Redis;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis bitfield operations
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis bitfield endpoints to the application
    /// </summary>
    public static void MapBitfieldEndpoints(this WebApplication app)
    {
        var bitfield = app.MapGroup("/bitfield")
            .WithTags("12. Redis Bitfields")
            .WithOpenApi();

        bitfield.MapPost("/{key}/set", BitfieldSet)
            .WithName("BitfieldSet")
            .WithSummary("Set bitfield value")
            .WithDescription("Sets a value in a bitfield at the specified offset")
            .Accepts<BitfieldSetRequest>("application/json")
            .Produces<NumericResult>(StatusCodes.Status200OK);

        bitfield.MapPost("/{key}/get", BitfieldGet)
            .WithName("BitfieldGet")
            .WithSummary("Get bitfield value")
            .WithDescription("Gets a value from a bitfield at the specified offset")
            .Accepts<BitfieldGetRequest>("application/json")
            .Produces<NumericResult>(StatusCodes.Status200OK);

        bitfield.MapPost("/{key}/incrby", BitfieldIncrBy)
            .WithName("BitfieldIncrBy")
            .WithSummary("Increment bitfield value")
            .WithDescription("Increments a value in a bitfield at the specified offset")
            .Accepts<BitfieldIncrByRequest>("application/json")
            .Produces<NumericResult>(StatusCodes.Status200OK);

        bitfield.MapPost("/{key}/operations", BitfieldMultiOperation)
            .WithName("BitfieldMultiOperation")
            .WithSummary("Multiple bitfield operations")
            .WithDescription("Executes multiple bitfield operations in a single command")
            .Accepts<BitfieldMultiRequest>("application/json")
            .Produces<BitfieldMultiResult>(StatusCodes.Status200OK);

        bitfield.MapGet("/{key}/info", BitfieldInfo)
            .WithName("BitfieldInfo")
            .WithSummary("Get bitfield info")
            .WithDescription("Gets information about the bitfield")
            .Produces<BitfieldInfoResult>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> BitfieldSet(string key, [FromBody] BitfieldSetRequest request, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            // Note: This is a simulation of bitfield operations
            // In production, you'd use Redis BITFIELD command with proper type handling
            var result = await db.StringBitPositionAsync(key, request.Value > 0, request.Offset, request.Offset + request.Width - 1);
            return TypedResults.Ok(new NumericResult(result));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error in bitfield set: {ex.Message}"));
        }
    }

    private static async Task<IResult> BitfieldGet(string key, [FromBody] BitfieldGetRequest request, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            // Note: This is a simulation of bitfield operations
            var bit = await db.StringGetBitAsync(key, request.Offset);
            return TypedResults.Ok(new NumericResult(bit ? 1 : 0));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error in bitfield get: {ex.Message}"));
        }
    }

    private static async Task<IResult> BitfieldIncrBy(string key, [FromBody] BitfieldIncrByRequest request, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            // Note: This is a simulation - in production you'd use proper BITFIELD INCRBY
            var currentValue = await db.StringGetAsync($"{key}:bitfield:{request.Offset}");
            long current = currentValue.HasValue ? (long)currentValue : 0;
            long newValue = current + request.Increment;
            await db.StringSetAsync($"{key}:bitfield:{request.Offset}", newValue);
            return TypedResults.Ok(new NumericResult(newValue));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error in bitfield increment: {ex.Message}"));
        }
    }

    private static async Task<IResult> BitfieldMultiOperation(string key, [FromBody] BitfieldMultiRequest request, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var results = new List<long>();

            // Note: This is a simulation - in production you'd use a single BITFIELD command
            foreach (var op in request.Operations)
            {
                switch (op.Operation.ToLower())
                {
                    case "get":
                        var bit = await db.StringGetBitAsync(key, op.Offset);
                        results.Add(bit ? 1 : 0);
                        break;
                    case "set":
                        await db.StringSetBitAsync(key, op.Offset, op.Value > 0);
                        results.Add(op.Value);
                        break;
                    case "incrby":
                        var currentValue = await db.StringGetAsync($"{key}:bitfield:{op.Offset}");
                        long current = currentValue.HasValue ? (long)currentValue : 0;
                        long newValue = current + op.Value;
                        await db.StringSetAsync($"{key}:bitfield:{op.Offset}", newValue);
                        results.Add(newValue);
                        break;
                }
            }

            return TypedResults.Ok(new BitfieldMultiResult(results.ToArray()));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error in bitfield multi-operation: {ex.Message}"));
        }
    }

    private static async Task<IResult> BitfieldInfo(string key, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var exists = await db.KeyExistsAsync(key);
            var length = exists ? await db.StringLengthAsync(key) : 0;

            return TypedResults.Ok(new BitfieldInfoResult(
                key,
                exists,
                length,
                length * 8, // bits
                "Simulated bitfield info"
            ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting bitfield info: {ex.Message}"));
        }
    }
}
