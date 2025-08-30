namespace RedisPlayground.ApiService.Models;

// Redis Lists models
public record ListPushRequest(string[] Values, string Direction = "right"); // "left" or "right"
public record ListPopRequest(string Direction = "right"); // "left" or "right"
public record ListResult(string Key, string[] Values);
