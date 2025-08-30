namespace RedisPlayground.ApiService.Models;

// Redis Geospatial models
public record GeoAddRequest(double Longitude, double Latitude, string Member);
public record GeoLocation(string Member, double Distance, string Unit);
public record GeoRadiusResult(string Key, GeoLocation[] Locations);
