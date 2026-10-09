using Microsoft.AspNetCore.SignalR;

namespace Server;

public interface IConnectionManager
{
    string IdentifyUserByConnection(HubCallerContext context);
    void Register(string playerId, string connectionId);
    void Unregister(string playerId);
    string? GetConnectionId(string playerId);
    IReadOnlyList<string> GetConnectionsFor(string playerId);
}