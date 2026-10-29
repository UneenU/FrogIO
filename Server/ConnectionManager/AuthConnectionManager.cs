using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace Server.ConnectionManager;

public class AuthConnectionManager(IConnectionMultiplexer redis) : IConnectionManager
{
    private readonly IDatabase _database = redis.GetDatabase();
    private static string UserKey(int userId) => $"user-connection-id:{userId}";
    

    public int IdentifyUserByConnection(HubCallerContext context)
    {
        int.TryParse(context.UserIdentifier, out var userId);
        if (userId == 0) throw new HubException("Невозможно опознать пользователя");
        return userId;
    }

    public async Task Register(int playerId, string connectionId)
    {
        await _database.StringSetAsync(UserKey(playerId), connectionId);
    }

    public async Task Unregister(int playerId)
    {
        await _database.KeyDeleteAsync(UserKey(playerId));
    }

    public async Task<string?> GetConnectionId(int playerId)
    {
        var item = await _database.StringGetAsync(UserKey(playerId));
        if (item.HasValue) return item.ToString();
        return null;
    }
}