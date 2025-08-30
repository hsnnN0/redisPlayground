namespace RedisPlayground.ApiService.Models;

// Redis Sorted Sets / Leaderboards models
public record SortedSetAddRequest(string Member, double Score);
public record LeaderboardEntry(int Rank, string Member, double Score);
public record LeaderboardResult(string Key, LeaderboardEntry[] Entries);
public record RankResult(string Member, long Rank);
