using RedisPlayground.ApiService.Models;
using StackExchange.Redis;
using System.Text;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis developer tools and utilities
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis developer tools endpoints to the application
    /// </summary>
    public static void MapDeveloperToolsEndpoints(this WebApplication app)
    {
        var devtools = app.MapGroup("/devtools")
            .WithTags("15. Developer Tools")
            .WithOpenApi();

        devtools.MapPost("/command", ExecuteRedisCommand)
            .WithName("ExecuteRedisCommand")
            .WithSummary("Execute raw Redis command")
            .WithDescription("Executes a raw Redis command for debugging and development")
            .Accepts<RedisCommandRequest>("application/json")
            .Produces<RedisCommandResult>(StatusCodes.Status200OK);

        devtools.MapPost("/lua", ExecuteLuaScript)
            .WithName("ExecuteLuaScript")
            .WithSummary("Execute Lua script")
            .WithDescription("Executes a Lua script on the Redis server")
            .Accepts<LuaScriptRequest>("application/json")
            .Produces<LuaScriptResult>(StatusCodes.Status200OK);

        devtools.MapGet("/ping", PingRedis)
            .WithName("PingRedis")
            .WithSummary("Ping Redis server")
            .WithDescription("Tests connectivity to Redis server")
            .Produces<PingResult>(StatusCodes.Status200OK);

        devtools.MapPost("/debug/key", DebugKey)
            .WithName("DebugKey")
            .WithSummary("Debug key information")
            .WithDescription("Gets detailed debugging information about a key")
            .Accepts<DebugKeyRequest>("application/json")
            .Produces<DebugKeyResult>(StatusCodes.Status200OK);

        devtools.MapPost("/monitor", StartMonitoring)
            .WithName("StartMonitoring")
            .WithSummary("Start command monitoring")
            .WithDescription("Starts monitoring Redis commands (simulation)")
            .Accepts<MonitorRequest>("application/json")
            .Produces<MonitorResult>(StatusCodes.Status200OK);

        devtools.MapDelete("/flush/{database?}", FlushDatabase)
            .WithName("FlushDatabase")
            .WithSummary("Flush database")
            .WithDescription("Flushes a specific database or all databases (USE WITH CAUTION)")
            .Produces<OperationResult>(StatusCodes.Status200OK);

        devtools.MapPost("/export", ExportKeys)
            .WithName("ExportKeys")
            .WithSummary("Export keys")
            .WithDescription("Exports keys matching a pattern to JSON format")
            .Accepts<ExportKeysRequest>("application/json")
            .Produces<ExportResult>(StatusCodes.Status200OK);

        devtools.MapPost("/import", ImportKeys)
            .WithName("ImportKeys")
            .WithSummary("Import keys")
            .WithDescription("Imports keys from JSON format")
            .Accepts<ImportKeysRequest>("application/json")
            .Produces<ImportResult>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> ExecuteRedisCommand(RedisCommandRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();

            // Note: This is a simplified implementation
            // In production, you'd want proper command parsing and security checks
            var parts = request.Command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return TypedResults.BadRequest(new OperationResult(false, "Invalid command"));
            }

            var command = parts[0].ToUpper();
            var args = parts.Skip(1).ToArray();

            object? result = null;

            switch (command)
            {
                case "GET":
                    if (args.Length >= 1)
                        result = await db.StringGetAsync(args[0]);
                    break;
                case "SET":
                    if (args.Length >= 2)
                        result = await db.StringSetAsync(args[0], args[1]);
                    break;
                case "DEL":
                    if (args.Length >= 1)
                        result = await db.KeyDeleteAsync(args[0]);
                    break;
                case "EXISTS":
                    if (args.Length >= 1)
                        result = await db.KeyExistsAsync(args[0]);
                    break;
                case "TTL":
                    if (args.Length >= 1)
                        result = await db.KeyTimeToLiveAsync(args[0]);
                    break;
                default:
                    return TypedResults.BadRequest(new OperationResult(false, $"Command '{command}' not supported in this demo"));
            }

            return TypedResults.Ok(new RedisCommandResult(
                request.Command,
                result?.ToString() ?? "(null)",
                true,
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new RedisCommandResult(
                request.Command,
                ex.Message,
                false,
                DateTime.UtcNow
            ));
        }
    }

    private static async Task<IResult> ExecuteLuaScript(LuaScriptRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();

            var keys = request.Keys?.Select(k => (RedisKey)k).ToArray() ?? Array.Empty<RedisKey>();
            var values = request.Args?.Select(a => (RedisValue)a).ToArray() ?? Array.Empty<RedisValue>();

            var result = await db.ScriptEvaluateAsync(request.Script, keys, values);

            return TypedResults.Ok(new LuaScriptResult(
                request.Script,
                result.ToString(),
                true,
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new LuaScriptResult(
                request.Script,
                ex.Message,
                false,
                DateTime.UtcNow
            ));
        }
    }

    private static async Task<IResult> PingRedis(IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await db.PingAsync();
            stopwatch.Stop();

            return TypedResults.Ok(new PingResult(
                true,
                stopwatch.ElapsedMilliseconds,
                "PONG",
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new PingResult(
                false,
                0,
                ex.Message,
                DateTime.UtcNow
            ));
        }
    }

    private static async Task<IResult> DebugKey(DebugKeyRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();

            var exists = await db.KeyExistsAsync(request.Key);
            if (!exists)
            {
                return TypedResults.NotFound();
            }

            var type = await db.KeyTypeAsync(request.Key);
            var ttl = await db.KeyTimeToLiveAsync(request.Key);
            var size = 0L;
            var encoding = "unknown";
            var refCount = 0;

            // Get size based on type
            switch (type)
            {
                case RedisType.String:
                    size = await db.StringLengthAsync(request.Key);
                    encoding = "raw";
                    break;
                case RedisType.List:
                    size = await db.ListLengthAsync(request.Key);
                    encoding = "quicklist";
                    break;
                case RedisType.Set:
                    size = await db.SetLengthAsync(request.Key);
                    encoding = "hashtable";
                    break;
                case RedisType.SortedSet:
                    size = await db.SortedSetLengthAsync(request.Key);
                    encoding = "skiplist";
                    break;
                case RedisType.Hash:
                    size = await db.HashLengthAsync(request.Key);
                    encoding = "hashtable";
                    break;
            }

            return TypedResults.Ok(new DebugKeyResult(
                request.Key,
                type.ToString(),
                size,
                ttl,
                encoding,
                refCount,
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error debugging key: {ex.Message}"));
        }
    }

    private static Task<IResult> StartMonitoring(MonitorRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            // Note: This is a simulation of Redis MONITOR command
            // In a real implementation, you'd need to handle the streaming nature of MONITOR
            var monitorData = new[]
            {
                new MonitorEvent(DateTime.UtcNow, "127.0.0.1:6379", 0, "GET", new[] { "test_key" }),
                new MonitorEvent(DateTime.UtcNow.AddSeconds(-1), "127.0.0.1:6379", 0, "SET", new[] { "test_key", "value" }),
                new MonitorEvent(DateTime.UtcNow.AddSeconds(-2), "127.0.0.1:6379", 0, "DEL", new[] { "old_key" })
            };

            var limitedEvents = monitorData.Take(request.MaxEvents).ToArray();

            return Task.FromResult<IResult>(TypedResults.Ok(new MonitorResult(
                limitedEvents,
                "Monitoring simulation started",
                DateTime.UtcNow
            )));
        }
        catch (Exception ex)
        {
            return Task.FromResult<IResult>(TypedResults.BadRequest(new OperationResult(false, $"Error starting monitoring: {ex.Message}")));
        }
    }

    private static async Task<IResult> FlushDatabase(int? database, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints()[0]);

            if (database.HasValue)
            {
                await server.FlushDatabaseAsync(database.Value);
                return TypedResults.Ok(new OperationResult(true, $"Database {database} flushed successfully"));
            }
            else
            {
                await server.FlushAllDatabasesAsync();
                return TypedResults.Ok(new OperationResult(true, "All databases flushed successfully"));
            }
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error flushing database: {ex.Message}"));
        }
    }

    private static async Task<IResult> ExportKeys(ExportKeysRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints()[0]);
            var db = redis.GetDatabase();

            var keys = server.Keys(pattern: request.Pattern).Take(request.MaxKeys).ToList();
            var exportData = new List<KeyExport>();

            foreach (var key in keys)
            {
                var type = await db.KeyTypeAsync(key);
                var ttl = await db.KeyTimeToLiveAsync(key);
                string? value = null;

                switch (type)
                {
                    case RedisType.String:
                        value = await db.StringGetAsync(key);
                        break;
                    case RedisType.List:
                        var listValues = await db.ListRangeAsync(key);
                        value = string.Join(",", listValues.Select(v => v.ToString()));
                        break;
                    case RedisType.Set:
                        var setValues = await db.SetMembersAsync(key);
                        value = string.Join(",", setValues.Select(v => v.ToString()));
                        break;
                    case RedisType.Hash:
                        var hashValues = await db.HashGetAllAsync(key);
                        value = string.Join(",", hashValues.Select(kv => $"{kv.Name}:{kv.Value}"));
                        break;
                    case RedisType.SortedSet:
                        var sortedSetValues = await db.SortedSetRangeByRankWithScoresAsync(key);
                        value = string.Join(",", sortedSetValues.Select(sv => $"{sv.Element}:{sv.Score}"));
                        break;
                }

                exportData.Add(new KeyExport(key!, type.ToString(), value ?? "", ttl));
            }

            return TypedResults.Ok(new ExportResult(
                exportData.ToArray(),
                exportData.Count,
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error exporting keys: {ex.Message}"));
        }
    }

    private static async Task<IResult> ImportKeys(ImportKeysRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var imported = 0;
            var errors = new List<string>();

            foreach (var keyData in request.Keys)
            {
                try
                {
                    var expiry = keyData.Ttl?.TotalMilliseconds > 0 ? keyData.Ttl : null;

                    switch (keyData.Type.ToLower())
                    {
                        case "string":
                            await db.StringSetAsync(keyData.Key, keyData.Value, expiry);
                            break;
                        case "list":
                            var listValues = keyData.Value.Split(',');
                            await db.ListRightPushAsync(keyData.Key, listValues.Select(v => (RedisValue)v).ToArray());
                            if (expiry.HasValue)
                                await db.KeyExpireAsync(keyData.Key, expiry);
                            break;
                        case "set":
                            var setValues = keyData.Value.Split(',');
                            await db.SetAddAsync(keyData.Key, setValues.Select(v => (RedisValue)v).ToArray());
                            if (expiry.HasValue)
                                await db.KeyExpireAsync(keyData.Key, expiry);
                            break;
                        case "hash":
                            var hashPairs = keyData.Value.Split(',')
                                .Select(kv => kv.Split(':'))
                                .Where(parts => parts.Length == 2)
                                .Select(parts => new HashEntry(parts[0], parts[1]))
                                .ToArray();
                            await db.HashSetAsync(keyData.Key, hashPairs);
                            if (expiry.HasValue)
                                await db.KeyExpireAsync(keyData.Key, expiry);
                            break;
                        default:
                            errors.Add($"Unsupported type '{keyData.Type}' for key '{keyData.Key}'");
                            continue;
                    }

                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error importing key '{keyData.Key}': {ex.Message}");
                }
            }

            return TypedResults.Ok(new ImportResult(
                imported,
                errors.ToArray(),
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error importing keys: {ex.Message}"));
        }
    }
}
