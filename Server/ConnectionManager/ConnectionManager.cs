using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace Server.ConnectionManager;

public class ConnectionManager : IConnectionManager
{
    private static ConcurrentDictionary<string, int> _connectionUserMap = new();
    private static ConcurrentDictionary<int, string> _userConnectionMap = new();

    public int IdentifyUserByConnection(HubCallerContext context)
    {
        if (_connectionUserMap.TryGetValue(context.ConnectionId, out int userId)) return userId;
        
        userId = _connectionUserMap.Count + 1;
        _connectionUserMap.TryAdd(context.ConnectionId, userId);
        _userConnectionMap.TryAdd(userId, context.ConnectionId);
        return userId;
    }

    public void Register(int playerId, string connectionId)
    {
        throw new NotImplementedException();
    }

    public void Unregister(int playerId)
    {
        throw new NotImplementedException();
    }

    public string GetConnectionId(int playerId)
    {
        return _userConnectionMap[playerId];
    }

    public IReadOnlyList<string> GetConnectionsFor(int playerId)
    {
        throw new NotImplementedException();
    }
}