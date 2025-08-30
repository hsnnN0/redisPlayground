using RedisPlayground.ApiService.Endpoints;

namespace RedisPlayground.ApiService.Extensions;

/// <summary>
/// Extension methods for mapping Redis endpoints
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Maps all Redis endpoints to the application
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication MapRedisEndpoints(this WebApplication app)
    {
        // Map all endpoint groups
        app.MapBasicCacheEndpoints();
        app.MapStringEndpoints();
        app.MapHashEndpoints();
        app.MapListEndpoints();
        app.MapSetEndpoints();
        app.MapSortedSetEndpoints();
        app.MapPubSubEndpoints();
        app.MapStreamEndpoints();
        app.MapGeospatialEndpoints();
        app.MapJsonEndpoints();
        app.MapBitfieldEndpoints();
        app.MapLockEndpoints();
        app.MapAnalyticsEndpoints();
        app.MapDeveloperToolsEndpoints();

        return app;
    }
}
