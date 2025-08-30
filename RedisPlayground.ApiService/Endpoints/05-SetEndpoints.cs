using RedisPlayground.ApiService.Models;
using StackExchange.Redis;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis Set operations
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis Set endpoints to the application
    /// </summary>
    public static void MapSetEndpoints(this WebApplication app)
    {
        var sets = app.MapGroup("/sets")
            .WithTags("5. Redis Sets")
            .WithOpenApi();

        sets.MapPost("/{key}/add", AddToSet)
            .WithName("AddToSet")
            .WithSummary("Add to set")
            .WithDescription("Adds one or more members to a set")
            .Accepts<SetAddRequest>("application/json")
            .Produces<NumericResult>(StatusCodes.Status200OK);

        sets.MapGet("/{key}", GetSet)
            .WithName("GetSet")
            .WithSummary("Get set members")
            .WithDescription("Gets all members of a set")
            .Produces<SetResult>(StatusCodes.Status200OK);

        sets.MapPost("/{key}/ismember", IsSetMember)
            .WithName("IsSetMember")
            .WithSummary("Check set membership")
            .WithDescription("Checks if a value is a member of a set")
            .Accepts<SetMemberRequest>("application/json")
            .Produces<BooleanResult>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> AddToSet(string key, SetAddRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var result = await db.SetAddAsync(key, request.Values.Select(v => (RedisValue)v).ToArray());
            return TypedResults.Ok(new NumericResult(result));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error adding to set: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetSet(string key, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var members = await db.SetMembersAsync(key);
            var result = members.Select(m => m.ToString()).ToArray();
            return TypedResults.Ok(new SetResult(key, result));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting set: {ex.Message}"));
        }
    }

    private static async Task<IResult> IsSetMember(string key, SetMemberRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var isMember = await db.SetContainsAsync(key, request.Value);
            return TypedResults.Ok(new BooleanResult(isMember));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error checking set membership: {ex.Message}"));
        }
    }
}
