using RedisPlayground.ApiService.Models;
using StackExchange.Redis;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis Pub/Sub operations
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis Pub/Sub endpoints to the application
    /// </summary>
    public static void MapPubSubEndpoints(this WebApplication app)
    {
        var pubsub = app.MapGroup("/pubsub")
            .WithTags("7. Redis Pub/Sub")
            .WithOpenApi();

        pubsub.MapPost("/publish/{channel}", PublishMessage)
            .WithName("PublishMessage")
            .WithSummary("Publish message")
            .WithDescription("Publishes a message to a Redis channel")
            .Accepts<PublishRequest>("application/json")
            .Produces<NumericResult>(StatusCodes.Status200OK);

        pubsub.MapPost("/subscribe", SubscribeToChannels)
            .WithName("SubscribeToChannels")
            .WithSummary("Subscribe to channels")
            .WithDescription("Subscribes to Redis channels and returns messages (polling-based)")
            .Accepts<SubscribeRequest>("application/json")
            .Produces<SubscriptionResult>(StatusCodes.Status200OK);

        pubsub.MapGet("/subscribe/{channel}/stream", SubscribeToChannelStream)
            .WithName("SubscribeToChannelStream")
            .WithSummary("Subscribe to channel stream")
            .WithDescription("Subscribes to a Redis channel using Server-Sent Events for real-time updates")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream");
    }

    private static async Task<IResult> PublishMessage(string channel, PublishRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var subscriber = redis.GetSubscriber();
            var result = await subscriber.PublishAsync(RedisChannel.Literal(channel), request.Message);
            return TypedResults.Ok(new NumericResult(result));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error publishing message: {ex.Message}"));
        }
    }

    private static async Task<IResult> SubscribeToChannels(SubscribeRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var subscriber = redis.GetSubscriber();
            var receivedMessages = new List<MessageReceived>();
            var timeout = request.TimeoutSeconds ?? 5; // Default 5 seconds timeout
            
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cancellationSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            var subscriptionTasks = new List<Task>();
            
            foreach (var channel in request.Channels)
            {
                var subscriptionTask = Task.Run(async () =>
                {
                    await subscriber.SubscribeAsync(RedisChannel.Literal(channel), (ch, message) =>
                    {
                        if (!cancellationSource.Token.IsCancellationRequested)
                        {
                            lock (receivedMessages)
                            {
                                receivedMessages.Add(new MessageReceived(ch!, message!, DateTime.UtcNow));
                            }
                        }
                    });
                }, cancellationSource.Token);
                
                subscriptionTasks.Add(subscriptionTask);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(timeout), cancellationSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when timeout occurs
            }

            // Unsubscribe from all channels
            foreach (var channel in request.Channels)
            {
                await subscriber.UnsubscribeAsync(RedisChannel.Literal(channel));
            }

            var timedOut = cancellationSource.Token.IsCancellationRequested && !ct.IsCancellationRequested;
            return TypedResults.Ok(new SubscriptionResult(receivedMessages.ToArray(), timedOut));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error subscribing to channels: {ex.Message}"));
        }
    }

    private static async Task SubscribeToChannelStream(string channel, HttpContext context, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";
        
        // Add CORS headers for SSE
        context.Response.Headers.AccessControlAllowOrigin = "*";

        try
        {
            var subscriber = redis.GetSubscriber();
            
            await subscriber.SubscribeAsync(RedisChannel.Literal(channel), async (ch, message) =>
            {
                try
                {
                    if (!ct.IsCancellationRequested)
                    {
                        var messageData = new MessageReceived(ch!, message!, DateTime.UtcNow);
                        var json = System.Text.Json.JsonSerializer.Serialize(messageData, new System.Text.Json.JsonSerializerOptions 
                        { 
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase 
                        });
                        
                        await context.Response.WriteAsync($"data: {json}\n\n", ct);
                        await context.Response.Body.FlushAsync(ct);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't break the stream
                    await context.Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n", ct);
                    await context.Response.Body.FlushAsync(ct);
                }
            });

            // Send initial connection event
            await context.Response.WriteAsync("event: connected\ndata: {\"message\":\"Connected to channel\"}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);

            // Keep the connection alive until cancellation
            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                // Expected when client disconnects
            }
            finally
            {
                await subscriber.UnsubscribeAsync(RedisChannel.Literal(channel));
            }
        }
        catch (Exception ex)
        {
            await context.Response.WriteAsync($"event: error\ndata: {{\"error\":\"{ex.Message}\"}}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }
    }
}
