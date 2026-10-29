using Core.Match;
using Core.Systems;
using Microsoft.AspNetCore.SignalR;
using Server.ConnectionManager;
using Server.Hub;
using Server.Match.MatchService;

namespace Server.Match;

public class MatchNotifier(
    IConnectionManager connectionManager,
    IHubContext<GameHub, IGameHubClient> hubContext)
{
    public async Task NotifyPlayersAboutMatchEnding(string matchId, MatchStatistic statistic)
    {
        await hubContext.Clients
            .Group(GameHub.GetMatchGroupName(matchId))
            .MatchEnded(statistic);
    }

    public async Task NotifyPlayersAboutMatchState(string matchId, MatchState state)
    {
        await hubContext.Clients
            .Group(GameHub.GetMatchGroupName(matchId))
            .UpdateWorldState(state);
    }

    public async Task NotifyPlayerAboutLevelUp(int playerId, LevelListPosition[] levelListPositions)
    {
        var connection = await connectionManager.GetConnectionId(playerId);
        await hubContext.Clients
            .Client(connection!)
            .LevelUp(levelListPositions);
    }
}