namespace RedisPlayground.ApiService.Models;

// Common result types
public record OperationResult(bool Success, string Message);
public record StringResult(string Key, string Value);
public record NumericResult(long Value);
public record BooleanResult(bool Value);
