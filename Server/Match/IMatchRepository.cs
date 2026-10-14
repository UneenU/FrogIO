using System.Collections.Concurrent;
using Server.Lobby.LobbyRepository;

namespace Server.Match;

public interface IMatchRepository
{
    ValueTask<string?> FindUserMatchAsync(int userId);
    ValueTask SetUserMatchAsync(int userId, string matchId);
}

public class MatchRepository() : IMatchRepository
{
    private static ConcurrentDictionary<string, int> _matchPlayerMap = [];
    private static ConcurrentDictionary<int, string> _playerMatchMap = [];
    public async ValueTask<string?> FindUserMatchAsync(int userId)
    {
        return _playerMatchMap[userId];
    }

    public async ValueTask SetUserMatchAsync(int userId, string matchId)
    {
        _matchPlayerMap.TryAdd(matchId, userId);
        _playerMatchMap.TryAdd(userId, matchId);
    }
}