using Microsoft.AspNetCore.Mvc;
using RedisPlayground.ApiService.Models;
using StackExchange.Redis;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis Sorted Set operations and Leaderboards
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis Sorted Set endpoints to the application
    /// </summary>
    public static void MapSortedSetEndpoints(this WebApplication app)
    {
        var sortedSets = app.MapGroup("/sorted-sets")
            .WithTags("6. Redis Sorted Sets & Leaderboards")
            .WithOpenApi();

        sortedSets.MapPost("/{key}/add", AddToSortedSet)
            .WithName("AddToSortedSet")
            .WithSummary("Add to sorted set")
            .WithDescription("Adds a member with score to a sorted set")
            .Accepts<SortedSetAddRequest>("application/json")
            .Produces<NumericResult>(StatusCodes.Status200OK);

        sortedSets.MapGet("/{key}/leaderboard", GetLeaderboard)
            .WithName("GetLeaderboard")
            .WithSummary("Get leaderboard")
            .WithDescription("Gets the top N members from a sorted set (leaderboard)")
            .Produces<LeaderboardResult>(StatusCodes.Status200OK);

        sortedSets.MapGet("/{key}/rank/{member}", GetMemberRank)
            .WithName("GetMemberRank")
            .WithSummary("Get member rank")
            .WithDescription("Gets the rank (position) of a member in a sorted set")
            .Produces<RankResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> AddToSortedSet(string key, [FromBody] SortedSetAddRequest request, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var success = await db.SortedSetAddAsync(key, request.Member, request.Score);
            return TypedResults.Ok(new NumericResult(success ? 1 : 0));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error adding to sorted set: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetLeaderboard(string key, [FromServices] IConnectionMultiplexer redis, int count = 10, bool descending = true, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var members = descending
                ? await db.SortedSetRangeByRankWithScoresAsync(key, 0, count - 1, Order.Descending)
                : await db.SortedSetRangeByRankWithScoresAsync(key, 0, count - 1, Order.Ascending);

            var entries = members.Select((m, index) => new LeaderboardEntry(index + 1, m.Element!, m.Score)).ToArray();
            return TypedResults.Ok(new LeaderboardResult(key, entries));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting leaderboard: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetMemberRank(string key, string member, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var rank = await db.SortedSetRankAsync(key, member, Order.Descending);
            if (!rank.HasValue)
                return TypedResults.NotFound();

            return TypedResults.Ok(new RankResult(member, rank.Value + 1)); // Convert 0-based to 1-based rank
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting member rank: {ex.Message}"));
        }
    }
}
