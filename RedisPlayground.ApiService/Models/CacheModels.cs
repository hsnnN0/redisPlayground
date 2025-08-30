namespace RedisPlayground.ApiService.Models;

// Basic Cache models
public record CacheEntryResult(string Key, string Value);
public record CacheKeyResult(string Key);
public record SetCacheRequest(string Value, int? ExpirationMinutes = 5);
