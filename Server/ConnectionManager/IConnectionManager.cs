using Microsoft.AspNetCore.SignalR;

namespace Server.ConnectionManager;

public interface IConnectionManager
{
    int IdentifyUserByConnection(HubCallerContext context);
    Task Register(int playerId, string connectionId);
    Task Unregister(int playerId);
    Task<string?> GetConnectionId(int playerId);
    
}