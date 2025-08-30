namespace RedisPlayground.ApiService.Models;

// Redis Strings models
public record SetStringRequest(string Value, int? ExpirationSeconds = null);
public record IncrementRequest(long Amount = 1);
