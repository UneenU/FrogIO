using Microsoft.AspNetCore.SignalR;

namespace Server;

public interface IConnectionManager
{
    int IdentifyUserByConnection(HubCallerContext context);
    void Register(int playerId, string connectionId);
    void Unregister(int playerId);
    string? GetConnectionId(int playerId);
    IReadOnlyList<string> GetConnectionsFor(int playerId);
}