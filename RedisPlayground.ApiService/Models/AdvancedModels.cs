namespace RedisPlayground.ApiService.Models;

/// <summary>
/// JSON operation models
/// </summary>
public record JsonSetRequest(object Value);
public record JsonPathRequest(string Path);
public record JsonPathSetRequest(string Path, object Value);
public record JsonResult(string Key, string Value);

/// <summary>
/// Bitfield operation models
/// </summary>
public record BitfieldSetRequest(int Offset, int Width, long Value);
public record BitfieldGetRequest(int Offset, int Width);
public record BitfieldIncrByRequest(int Offset, int Width, long Increment);
public record BitfieldOperation(string Operation, int Offset, long Value);
public record BitfieldMultiRequest(BitfieldOperation[] Operations);
public record BitfieldMultiResult(long[] Results);
public record BitfieldInfoResult(string Key, bool Exists, long Length, long BitLength, string Description);

/// <summary>
/// Lock operation models
/// </summary>
public record LockRequest(int TimeoutSeconds);
public record LockReleaseRequest(string Token);
public record LockExtendRequest(string Token, int ExtendBySeconds);
public record LockResult(bool Acquired, string Token, string LockName, DateTime? ExpiresAt);
public record LockStatusResult(string LockName, bool IsLocked, string? Token, DateTime? ExpiresAt);
public record LockInfo(string Name, string Token, DateTime? ExpiresAt);
public record ActiveLocksResult(LockInfo[] Locks);

/// <summary>
/// Analytics and monitoring models
/// </summary>
public record RedisInfoResult(string Version, string Uptime, string UsedMemory, string TotalConnections, DateTime Timestamp);
public record MemoryUsageResult(long UsedMemoryBytes, string UsedMemoryHuman, long MaxMemoryBytes, double FragmentationRatio, DateTime Timestamp);
public record ServerStatsResult(long TotalConnections, long TotalCommands, long KeyspaceHits, long KeyspaceMisses, DateTime Timestamp);
public record ClientConnection(string Id, string Address, string Name, long AgeSeconds, long IdleSeconds, int Database);
public record ClientInfoResult(ClientConnection[] Clients, DateTime Timestamp);
public record SlowLogRequest(int Count = 10);
public record SlowLogEntry(long Id, DateTime Timestamp, long DurationMicroseconds, string[] Command);
public record SlowLogResult(SlowLogEntry[] Entries);
public record DatabaseInfo(int Database, int Keys, int Expires, long AvgTtl);
public record KeyspaceInfoResult(DatabaseInfo[] Databases, DateTime Timestamp);
public record KeyPatternRequest(string Pattern, int MaxKeys = 100);
public record KeyAnalysis(string Key, string Type, long Size, TimeSpan? Ttl);
public record KeyPatternAnalysisResult(string Pattern, int MatchCount, KeyAnalysis[] Keys, DateTime Timestamp);

/// <summary>
/// Developer tools models
/// </summary>
public record RedisCommandRequest(string Command);
public record RedisCommandResult(string Command, string Result, bool Success, DateTime Timestamp);
public record LuaScriptRequest(string Script, string[]? Keys = null, string[]? Args = null);
public record LuaScriptResult(string Script, string Result, bool Success, DateTime Timestamp);
public record PingResult(bool Success, long ResponseTimeMs, string Message, DateTime Timestamp);
public record DebugKeyRequest(string Key);
public record DebugKeyResult(string Key, string Type, long Size, TimeSpan? Ttl, string Encoding, int RefCount, DateTime Timestamp);
public record MonitorRequest(int MaxEvents = 10);
public record MonitorEvent(DateTime Timestamp, string Client, int Database, string Command, string[] Args);
public record MonitorResult(MonitorEvent[] Events, string Message, DateTime Timestamp);
public record ExportKeysRequest(string Pattern = "*", int MaxKeys = 1000);
public record KeyExport(string Key, string Type, string Value, TimeSpan? Ttl);
public record ExportResult(KeyExport[] Keys, int Count, DateTime Timestamp);
public record ImportKeysRequest(KeyExport[] Keys);
public record ImportResult(int ImportedCount, string[] Errors, DateTime Timestamp);
