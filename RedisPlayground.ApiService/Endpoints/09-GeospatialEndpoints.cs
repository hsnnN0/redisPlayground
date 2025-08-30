using RedisPlayground.ApiService.Models;
using StackExchange.Redis;

namespace RedisPlayground.ApiService.Endpoints;

/// <summary>
/// Redis Geospatial operations
/// </summary>
public static partial class RedisEndpoints
{
    /// <summary>
    /// Maps all Redis Geospatial endpoints to the application
    /// </summary>
    public static void MapGeospatialEndpoints(this WebApplication app)
    {
        var geo = app.MapGroup("/geo")
            .WithTags("9. Redis Geospatial")
            .WithOpenApi();

        geo.MapPost("/{key}/add", AddGeoLocation)
            .WithName("AddGeoLocation")
            .WithSummary("Add geo location")
            .WithDescription("Adds a geographic location to a Redis geospatial index")
            .Accepts<GeoAddRequest>("application/json")
            .Produces<NumericResult>(StatusCodes.Status200OK);

        geo.MapGet("/{key}/radius", GetGeoRadius)
            .WithName("GetGeoRadius")
            .WithSummary("Get locations by radius")
            .WithDescription("Gets locations within a specified radius of a point")
            .Produces<Models.GeoRadiusResult>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> AddGeoLocation(string key, GeoAddRequest request, IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var success = await db.GeoAddAsync(key, request.Longitude, request.Latitude, request.Member);
            return TypedResults.Ok(new NumericResult(success ? 1 : 0));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error adding geo location: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetGeoRadius(string key, IConnectionMultiplexer redis, double longitude = 0, double latitude = 0, double radius = 1000, string unit = "m", CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var geoUnit = unit.ToLowerInvariant() switch
            {
                "km" => GeoUnit.Kilometers,
                "mi" => GeoUnit.Miles,
                "ft" => GeoUnit.Feet,
                _ => GeoUnit.Meters
            };

            var results = await db.GeoRadiusAsync(key, longitude, latitude, radius, geoUnit, -1, Order.Ascending, GeoRadiusOptions.WithDistance);
            var locations = results.Select(r => new GeoLocation(r.Member!, r.Distance ?? 0, unit)).ToArray();
            return TypedResults.Ok(new Models.GeoRadiusResult(key, locations));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new OperationResult(false, $"Error getting geo radius: {ex.Message}"));
        }
    }
}
