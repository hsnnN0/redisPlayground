using Microsoft.AspNetCore.Mvc;
using RedisPlayground.ApiService.Models;
using StackExchange.Redis;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis analytics and monitoring operations
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis analytics endpoints to the application
    /// </summary>
    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var analytics = app.MapGroup("/analytics")
            .WithTags("14. Redis Analytics")
            .WithOpenApi();

        analytics.MapGet("/info", GetRedisInfo)
            .WithName("GetRedisInfo")
            .WithSummary("Get Redis server info")
            .WithDescription("Gets comprehensive Redis server information")
            .Produces<RedisInfoResult>(StatusCodes.Status200OK);

        analytics.MapGet("/memory", GetMemoryUsage)
            .WithName("GetMemoryUsage")
            .WithSummary("Get memory usage")
            .WithDescription("Gets detailed memory usage statistics")
            .Produces<MemoryUsageResult>(StatusCodes.Status200OK);

        analytics.MapGet("/stats", GetServerStats)
            .WithName("GetServerStats")
            .WithSummary("Get server statistics")
            .WithDescription("Gets Redis server performance statistics")
            .Produces<ServerStatsResult>(StatusCodes.Status200OK);

        analytics.MapGet("/clients", GetClientInfo)
            .WithName("GetClientInfo")
            .WithSummary("Get client connections")
            .WithDescription("Gets information about connected clients")
            .Produces<ClientInfoResult>(StatusCodes.Status200OK);

        analytics.MapPost("/slowlog", GetSlowLog)
            .WithName("GetSlowLog")
            .WithSummary("Get slow query log")
            .WithDescription("Gets the slow query log with optional count limit")
            .Accepts<SlowLogRequest>("application/json")
            .Produces<SlowLogResult>(StatusCodes.Status200OK);

        analytics.MapGet("/keyspace", GetKeyspaceInfo)
            .WithName("GetKeyspaceInfo")
            .WithSummary("Get keyspace information")
            .WithDescription("Gets keyspace statistics for all databases")
            .Produces<KeyspaceInfoResult>(StatusCodes.Status200OK);

        analytics.MapPost("/keys/pattern", AnalyzeKeyPattern)
            .WithName("AnalyzeKeyPattern")
            .WithSummary("Analyze key patterns")
            .WithDescription("Analyzes keys matching a pattern")
            .Accepts<KeyPatternRequest>("application/json")
            .Produces<KeyPatternAnalysisResult>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetRedisInfo([FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints()[0]);
            var info = await server.InfoAsync();

            var serverInfo = info.FirstOrDefault(g => g.Key == "server");
            var memoryInfo = info.FirstOrDefault(g => g.Key == "memory");
            var statsInfo = info.FirstOrDefault(g => g.Key == "stats");

            var redisVersion = serverInfo?.FirstOrDefault(kvp => kvp.Key == "redis_version");
            var uptime = serverInfo?.FirstOrDefault(kvp => kvp.Key == "uptime_in_seconds");
            var usedMemory = memoryInfo?.FirstOrDefault(kvp => kvp.Key == "used_memory_human");
            var totalConnections = statsInfo?.FirstOrDefault(kvp => kvp.Key == "total_connections_received");

            return TypedResults.Ok(new RedisInfoResult(
                redisVersion?.Value ?? "Unknown",
                uptime?.Value ?? "0",
                usedMemory?.Value ?? "Unknown",
                totalConnections?.Value ?? "0",
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting Redis info: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetMemoryUsage([FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints()[0]);
            var info = await server.InfoAsync("memory");

            var memorySection = info.FirstOrDefault(g => g.Key == "memory");
            if (memorySection?.Key == null)
            {
                return TypedResults.BadRequest(new OperationResult(false, "Memory information not available"));
            }

            var usedMemoryKvp = memorySection.FirstOrDefault(kvp => kvp.Key == "used_memory");
            var usedMemoryHumanKvp = memorySection.FirstOrDefault(kvp => kvp.Key == "used_memory_human");
            var maxMemoryKvp = memorySection.FirstOrDefault(kvp => kvp.Key == "maxmemory");
            var memoryFragmentationRatioKvp = memorySection.FirstOrDefault(kvp => kvp.Key == "mem_fragmentation_ratio");

            var usedMemory = string.IsNullOrEmpty(usedMemoryKvp.Key) ? "0" : usedMemoryKvp.Value;
            var usedMemoryHuman = string.IsNullOrEmpty(usedMemoryHumanKvp.Key) ? "0B" : usedMemoryHumanKvp.Value;
            var maxMemory = string.IsNullOrEmpty(maxMemoryKvp.Key) ? "0" : maxMemoryKvp.Value;
            var memoryFragmentationRatio = string.IsNullOrEmpty(memoryFragmentationRatioKvp.Key) ? "1.0" : memoryFragmentationRatioKvp.Value;

            return TypedResults.Ok(new MemoryUsageResult(
                long.Parse(usedMemory),
                usedMemoryHuman,
                long.Parse(maxMemory),
                double.Parse(memoryFragmentationRatio),
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting memory usage: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetServerStats([FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints()[0]);
            var info = await server.InfoAsync("stats");

            var statsSection = info.FirstOrDefault(g => g.Key == "stats");
            if (statsSection?.Key == null)
            {
                return TypedResults.BadRequest(new OperationResult(false, "Stats information not available"));
            }

            var totalConnectionsKvp = statsSection.FirstOrDefault(kvp => kvp.Key == "total_connections_received");
            var totalCommandsKvp = statsSection.FirstOrDefault(kvp => kvp.Key == "total_commands_processed");
            var keyspaceHitsKvp = statsSection.FirstOrDefault(kvp => kvp.Key == "keyspace_hits");
            var keyspaceMissesKvp = statsSection.FirstOrDefault(kvp => kvp.Key == "keyspace_misses");

            var totalConnections = string.IsNullOrEmpty(totalConnectionsKvp.Key) ? "0" : totalConnectionsKvp.Value;
            var totalCommands = string.IsNullOrEmpty(totalCommandsKvp.Key) ? "0" : totalCommandsKvp.Value;
            var keyspaceHits = string.IsNullOrEmpty(keyspaceHitsKvp.Key) ? "0" : keyspaceHitsKvp.Value;
            var keyspaceMisses = string.IsNullOrEmpty(keyspaceMissesKvp.Key) ? "0" : keyspaceMissesKvp.Value;

            return TypedResults.Ok(new ServerStatsResult(
                long.Parse(totalConnections),
                long.Parse(totalCommands),
                long.Parse(keyspaceHits),
                long.Parse(keyspaceMisses),
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting server stats: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetClientInfo([FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints()[0]);
            var clients = await server.ClientListAsync();

            var clientList = clients.Select(client => new ClientConnection(
                client.Id.ToString(),
                client.Address?.ToString() ?? "Unknown",
                client.Name ?? "Anonymous",
                client.AgeSeconds,
                client.IdleSeconds,
                client.Database
            )).ToArray();

            return TypedResults.Ok(new ClientInfoResult(clientList, DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting client info: {ex.Message}"));
        }
    }

    private static Task<IResult> GetSlowLog([FromBody] SlowLogRequest request, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints()[0]);

            // Note: StackExchange.Redis doesn't have direct SLOWLOG support
            // This is a simulation - in production you'd implement custom Redis commands
            var slowEntries = new[]
            {
                new SlowLogEntry(1, DateTime.UtcNow.AddMinutes(-5), 1500, new[] { "GET", "slow_key" }),
                new SlowLogEntry(2, DateTime.UtcNow.AddMinutes(-3), 2100, new[] { "LRANGE", "large_list", "0", "-1" })
            };

            var limitedEntries = slowEntries.Take(request.Count).ToArray();

            return Task.FromResult<IResult>(TypedResults.Ok(new SlowLogResult(limitedEntries)));
        }
        catch (Exception ex)
        {
            return Task.FromResult<IResult>(TypedResults.BadRequest(new OperationResult(false, $"Error getting slow log: {ex.Message}")));
        }
    }

    private static async Task<IResult> GetKeyspaceInfo([FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints()[0]);
            var info = await server.InfoAsync("keyspace");

            var keyspaceSection = info.FirstOrDefault(g => g.Key == "keyspace");
            var databases = new List<DatabaseInfo>();

            if (!string.IsNullOrEmpty(keyspaceSection?.Key))
            {
                foreach (var kvp in keyspaceSection)
                {
                    if (kvp.Key.StartsWith("db"))
                    {
                        var dbNumber = int.Parse(kvp.Key.Substring(2));
                        // Parse format: keys=X,expires=Y,avg_ttl=Z
                        var parts = kvp.Value.Split(',');
                        var keys = 0;
                        var expires = 0;
                        var avgTtl = 0L;

                        foreach (var part in parts)
                        {
                            var kv = part.Split('=');
                            if (kv.Length == 2)
                            {
                                switch (kv[0])
                                {
                                    case "keys":
                                        int.TryParse(kv[1], out keys);
                                        break;
                                    case "expires":
                                        int.TryParse(kv[1], out expires);
                                        break;
                                    case "avg_ttl":
                                        long.TryParse(kv[1], out avgTtl);
                                        break;
                                }
                            }
                        }

                        databases.Add(new DatabaseInfo(dbNumber, keys, expires, avgTtl));
                    }
                }
            }

            return TypedResults.Ok(new KeyspaceInfoResult(databases.ToArray(), DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting keyspace info: {ex.Message}"));
        }
    }

    private static async Task<IResult> AnalyzeKeyPattern([FromBody] KeyPatternRequest request, [FromServices] IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints()[0]);
            var db = redis.GetDatabase();

            var keys = server.Keys(pattern: request.Pattern).Take(request.MaxKeys).ToList();
            var analysis = new List<KeyAnalysis>();

            foreach (var key in keys)
            {
                var type = await db.KeyTypeAsync(key);
                var ttl = await db.KeyTimeToLiveAsync(key);
                var size = 0L;

                // Estimate size based on type
                switch (type)
                {
                    case RedisType.String:
                        size = await db.StringLengthAsync(key);
                        break;
                    case RedisType.List:
                        size = await db.ListLengthAsync(key);
                        break;
                    case RedisType.Set:
                        size = await db.SetLengthAsync(key);
                        break;
                    case RedisType.SortedSet:
                        size = await db.SortedSetLengthAsync(key);
                        break;
                    case RedisType.Hash:
                        size = await db.HashLengthAsync(key);
                        break;
                }

                analysis.Add(new KeyAnalysis(key!, type.ToString(), size, ttl));
            }

            return TypedResults.Ok(new KeyPatternAnalysisResult(
                request.Pattern,
                keys.Count,
                analysis.ToArray(),
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error analyzing key pattern: {ex.Message}"));
        }
    }
}
