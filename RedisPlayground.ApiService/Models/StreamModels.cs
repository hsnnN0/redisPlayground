namespace RedisPlayground.ApiService.Models;

// Redis Streams models
public record StreamAddRequest(Dictionary<string, string> Fields);
public record StreamIdResult(string Id);
public record StreamEntry(string Id, Dictionary<string, string> Fields);
public record StreamResult(string Stream, StreamEntry[] Entries);
