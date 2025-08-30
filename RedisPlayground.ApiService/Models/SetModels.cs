namespace RedisPlayground.ApiService.Models;

// Redis Sets models
public record SetAddRequest(string[] Values);
public record SetMemberRequest(string Value);
public record SetResult(string Key, string[] Members);
