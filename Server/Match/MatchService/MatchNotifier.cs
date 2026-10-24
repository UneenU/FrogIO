using Core.Match;
using Core.Systems;
using Microsoft.AspNetCore.SignalR;
using Server.ConnectionManager;
using Server.Hub;

namespace Server.Match;

public class MatchNotifier(
    IConnectionManager connectionManager,
    IHubContext<GameHub, IGameHubClient> hubContext)
{
    public async Task NotifyPlayersAboutMatchEnding(string matchId, MatchResult result)
    {
        await hubContext.Clients
            .Group(matchId)
            .MatchEnded(result);
    }

    public async Task NotifyPlayersAboutMatchState(string matchId, MatchState state)
    {
        await hubContext.Clients
            .Group(matchId)
            .UpdateWorldState(state);
    }

    public async Task NotifyPlayerAboutLevelUp(int playerId, LevelListPosition[] levelListPositions)
    {
        var connection = connectionManager.GetConnectionId(playerId);
        await hubContext.Clients
            .Client(connection!)
            .LevelUp(levelListPositions);
    }
}