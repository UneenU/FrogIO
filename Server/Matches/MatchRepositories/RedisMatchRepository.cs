using StackExchange.Redis;

namespace Server.Match.MatchRepository;

public class RedisMatchRepository(IConnectionMultiplexer redis) : IMatchRepository
{
    private readonly IDatabase _database = redis.GetDatabase();

    private static string UserMatchKey(int userId) => $"user-match:{userId}";
    private static string MatchKey(string matchId) => $"match:{matchId}";

    public async ValueTask<string?> FindUserMatchAsync(int userId)
    {
        var value = await _database.StringGetAsync(UserMatchKey(userId));
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async ValueTask SetUserMatchAsync(int userId, string matchId)
    {
        await _database.StringSetAsync(UserMatchKey(userId), matchId);
        await _database.SetAddAsync(MatchKey(matchId), userId);
    }

    public async Task RemoveMatchAsync(string matchId)
    {
        var usersIds = await _database.SetMembersAsync(MatchKey(matchId));
        foreach (var userId in usersIds)
        {
            await _database.KeyDeleteAsync(UserMatchKey(int.Parse(userId)));
        }
    }
}