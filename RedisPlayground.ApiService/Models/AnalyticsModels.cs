namespace RedisPlayground.ApiService.Models;

// Redis Analytics models (Bitmaps & HyperLogLog)
public record BitSetRequest(long Offset, bool Value);
public record HyperLogLogAddRequest(string[] Values);

// Probabilistic data structures models
public record BloomFilterAddRequest(string Element);
public record BloomFilterCheckRequest(string Element);
public record TopKAddRequest(string[] Elements, int K = 10);
public record TopKItem(string Element, long Count);
public record TopKResult(string Key, TopKItem[] Items);
