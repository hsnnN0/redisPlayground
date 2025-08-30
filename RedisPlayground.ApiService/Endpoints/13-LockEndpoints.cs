using Microsoft.AspNetCore.Mvc;
using RedisPlayground.ApiService.Models;
using StackExchange.Redis;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis distributed locking operations
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis lock endpoints to the application
    /// </summary>
    public static void MapLockEndpoints(this WebApplication app)
    {
        var locks = app.MapGroup("/locks")
            .WithTags("13. Redis Locks")
            .WithOpenApi();

        locks.MapPost("/{lockName}/acquire", AcquireLock)
            .WithName("AcquireLock")
            .WithSummary("Acquire distributed lock")
            .WithDescription("Acquires a distributed lock with optional timeout")
            .Accepts<LockRequest>("application/json")
            .Produces<LockResult>(StatusCodes.Status200OK);

        locks.MapPost("/{lockName}/release", ReleaseLock)
            .WithName("ReleaseLock")
            .WithSummary("Release distributed lock")
            .WithDescription("Releases a distributed lock using token")
            .Accepts<LockReleaseRequest>("application/json")
            .Produces<OperationResult>(StatusCodes.Status200OK);

        locks.MapGet("/{lockName}/status", GetLockStatus)
            .WithName("GetLockStatus")
            .WithSummary("Get lock status")
            .WithDescription("Gets the current status of a distributed lock")
            .Produces<LockStatusResult>(StatusCodes.Status200OK);

        locks.MapPost("/{lockName}/extend", ExtendLock)
            .WithName("ExtendLock")
            .WithSummary("Extend lock timeout")
            .WithDescription("Extends the timeout of an existing lock")
            .Accepts<LockExtendRequest>("application/json")
            .Produces<OperationResult>(StatusCodes.Status200OK);

        locks.MapGet("/active", GetActiveLocks)
            .WithName("GetActiveLocks")
            .WithSummary("Get all active locks")
            .WithDescription("Lists all currently active locks")
            .Produces<ActiveLocksResult>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> AcquireLock(string lockName, [FromBody] LockRequest request, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var lockKey = $"lock:{lockName}";
            var lockToken = Guid.NewGuid().ToString();
            var expiry = TimeSpan.FromSeconds(request.TimeoutSeconds);

            // Try to acquire the lock using SET with NX (not exists) and EX (expiry)
            var acquired = await db.StringSetAsync(lockKey, lockToken, expiry, When.NotExists);

            if (acquired)
            {
                return TypedResults.Ok(new LockResult(true, lockToken, lockName, DateTime.UtcNow.AddSeconds(request.TimeoutSeconds)));
            }
            else
            {
                // Lock is already held, get current holder info
                var currentToken = await db.StringGetAsync(lockKey);
                var ttl = await db.KeyTimeToLiveAsync(lockKey);
                var expiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : (DateTime?)null;

                return TypedResults.Ok(new LockResult(false, currentToken!, lockName, expiresAt));
            }
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error acquiring lock: {ex.Message}"));
        }
    }

    private static async Task<IResult> ReleaseLock(string lockName, [FromBody] LockReleaseRequest request, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var lockKey = $"lock:{lockName}";

            // Lua script to atomically check token and delete if it matches
            const string script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('DEL', KEYS[1])
                else
                    return 0
                end";

            var result = await db.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { request.Token });
            var released = (int)result == 1;

            return TypedResults.Ok(new OperationResult(released,
                released ? "Lock released successfully" : "Lock not found or token mismatch"));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error releasing lock: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetLockStatus(string lockName, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var lockKey = $"lock:{lockName}";

            var token = await db.StringGetAsync(lockKey);
            var isLocked = token.HasValue;
            var ttl = isLocked ? await db.KeyTimeToLiveAsync(lockKey) : null;
            var expiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : (DateTime?)null;

            return TypedResults.Ok(new LockStatusResult(lockName, isLocked, token!, expiresAt));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting lock status: {ex.Message}"));
        }
    }

    private static async Task<IResult> ExtendLock(string lockName, [FromBody] LockExtendRequest request, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var lockKey = $"lock:{lockName}";
            var expiry = TimeSpan.FromSeconds(request.ExtendBySeconds);

            // Lua script to atomically check token and extend expiry if it matches
            const string script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('EXPIRE', KEYS[1], ARGV[2])
                else
                    return 0
                end";

            var result = await db.ScriptEvaluateAsync(script,
                new RedisKey[] { lockKey },
                new RedisValue[] { request.Token, (int)expiry.TotalSeconds });

            var extended = (int)result == 1;

            return TypedResults.Ok(new OperationResult(extended,
                extended ? "Lock extended successfully" : "Lock not found or token mismatch"));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error extending lock: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetActiveLocks([FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var server = redis.GetServer(redis.GetEndPoints()[0]);

            var lockKeys = server.Keys(pattern: "lock:*").ToList();
            var activeLocks = new List<LockInfo>();

            foreach (var key in lockKeys)
            {
                var token = await db.StringGetAsync(key);
                if (token.HasValue)
                {
                    var ttl = await db.KeyTimeToLiveAsync(key);
                    var expiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : (DateTime?)null;
                    var lockName = key.ToString().Substring(5); // Remove "lock:" prefix

                    activeLocks.Add(new LockInfo(lockName, token!, expiresAt));
                }
            }

            return TypedResults.Ok(new ActiveLocksResult(activeLocks.ToArray()));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting active locks: {ex.Message}"));
        }
    }
}
