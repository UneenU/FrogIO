using StackExchange.Redis;

namespace Server.Match.MatchRepository;

public class RedisMatchRepository(IConnectionMultiplexer redis) : IMatchRepository
{
    private readonly IDatabase _database = redis.GetDatabase();

    private static string UserMatchKey(int userId) => $"user-match:{userId}";

    public async ValueTask<string?> FindUserMatchAsync(int userId)
    {
        var value = await _database.StringGetAsync(UserMatchKey(userId));
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async ValueTask SetUserMatchAsync(int userId, string matchId)
    {
        await _database.StringSetAsync(UserMatchKey(userId), matchId);
    }
}