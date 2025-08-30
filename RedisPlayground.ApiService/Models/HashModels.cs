namespace RedisPlayground.ApiService.Models;

// Redis Hashes models
public record HashFieldRequest(string Field, string Value);
public record HashResult(string Key, Dictionary<string, string> Fields);
