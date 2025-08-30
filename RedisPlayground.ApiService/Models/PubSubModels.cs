namespace RedisPlayground.ApiService.Models;

// Redis Pub/Sub models
public record PublishRequest(string Message);
public record SubscribeRequest(string[] Channels, int? TimeoutSeconds = null);
public record MessageReceived(string Channel, string Message, DateTime Timestamp);
public record SubscriptionResult(MessageReceived[] Messages, bool TimedOut = false);
