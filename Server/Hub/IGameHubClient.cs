using Core.Match;
using Core.Systems;
using Server.Match;
using Server.Match.MatchService;

namespace Server.Hub;

public interface IGameHubClient
{
    Task PlayerJoinedTheLobby(LobbyMemberDto player);
    Task PlayerLeftTheLobby(LobbyMemberDto player);
    Task PlayerReturnedInTheLobby(LobbyMemberDto player);
    Task LobbyWasClosed();
    Task PlayerReadyChanged(int playerId, bool isReady);
    
    Task YouWereDisconnected();

    Task MatchStarted(MatchInit initialState);
    Task UpdateWorldState(MatchState state);
    Task MatchEnded(MatchStatistic statistic);
    
    Task ReceiveChatMessage(string message);
    Task LevelUp(LevelListPosition[] levelListPositions);
}

public record LobbyMemberDto(
    int UserId,
    string Nickname,
    bool IsReady,
    string? ChosenFrog);