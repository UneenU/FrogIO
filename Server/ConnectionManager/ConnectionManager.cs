using Microsoft.AspNetCore.SignalR;

namespace Server;

public class ConnectionManager : IConnectionManager
{
    public string IdentifyUserByConnection(HubCallerContext context)
    {
        return context.ConnectionId;
    }

    public void Register(string playerId, string connectionId)
    {
        throw new NotImplementedException();
    }

    public void Unregister(string playerId)
    {
        throw new NotImplementedException();
    }

    public string GetConnectionId(string playerId)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<string> GetConnectionsFor(string playerId)
    {
        throw new NotImplementedException();
    }
}