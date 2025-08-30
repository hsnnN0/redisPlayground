using Microsoft.Extensions.Caching.Distributed;
using RedisPlayground.ApiService.Models;
using System.Text;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Basic cache operations using IDistributedCache
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all basic cache endpoints to the application
    /// </summary>
    public static void MapBasicCacheEndpoints(this WebApplication app)
    {
        var cacheGroup = app.MapGroup("/cache")
            .WithTags("1. Basic Cache")
            .WithOpenApi();

        cacheGroup.MapGet("/{key}", GetCacheEntry)
            .WithName("GetCacheEntry")
            .WithSummary("Get cache entry")
            .WithDescription("Retrieves a value from the cache by key")
            .Produces<CacheEntryResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        cacheGroup.MapPost("/{key}", SetCacheEntry)
            .WithName("SetCacheEntry")
            .WithSummary("Set cache entry")
            .WithDescription("Stores a value in the cache with a 5-minute expiration")
            .Accepts<string>("text/plain")
            .Produces<CacheKeyResult>(StatusCodes.Status201Created);

        cacheGroup.MapDelete("/{key}", DeleteCacheEntry)
            .WithName("DeleteCacheEntry")
            .WithSummary("Delete cache entry")
            .WithDescription("Removes a value from the cache")
            .Produces(StatusCodes.Status204NoContent);
    }

    /// <summary>
    /// Gets a cache entry by key
    /// </summary>
    private static async Task<IResult> GetCacheEntry(string key, IDistributedCache cache, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(key, ct);
        if (bytes is null)
            return TypedResults.NotFound();

        var value = Encoding.UTF8.GetString(bytes);
        return TypedResults.Ok(new CacheEntryResult(key, value));
    }

    /// <summary>
    /// Sets a cache entry with a value
    /// </summary>
    private static async Task<IResult> SetCacheEntry(string key, HttpRequest req, IDistributedCache cache, CancellationToken ct = default)
    {
        using var sr = new StreamReader(req.Body);
        var body = await sr.ReadToEndAsync();
        var bytes = Encoding.UTF8.GetBytes(body);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        await cache.SetAsync(key, bytes, options, ct);
        return TypedResults.Created($"/cache/{key}", new CacheKeyResult(key));
    }

    /// <summary>
    /// Deletes a cache entry by key
    /// </summary>
    private static async Task<IResult> DeleteCacheEntry(string key, IDistributedCache cache, CancellationToken ct = default)
    {
        await cache.RemoveAsync(key, ct);
        return TypedResults.NoContent();
    }
}
