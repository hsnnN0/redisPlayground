namespace RedisPlayground.ApiService.Models;

// Redis Time Series models
public record TimeSeriesCreateRequest(int? RetentionMs = null, Dictionary<string, string>? Labels = null);
public record TimeSeriesAddRequest(double Value, long? Timestamp = null);
public record TimeSeriesAddResult(long Timestamp);
public record TimeSeriesDataPoint(long Timestamp, double Value);
public record TimeSeriesResult(string Key, TimeSeriesDataPoint[] DataPoints);
public record TimeSeriesInfoResult(string Key, long TotalSamples, Dictionary<string, string> Metadata);
