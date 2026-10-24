using Core.Match;
using Core.Systems;
using Server.Match;

namespace Server.Hub;

public interface IGameHubClient
{
    Task PlayerJoinedTheLobby(LobbyMemberDto player);
    Task PlayerLeftTheLobby(LobbyMemberDto player);
    Task PlayerReturnedInTheLobby(LobbyMemberDto player);
    Task LobbyWasClosed();
    Task PlayerReadyChanged(int playerId, bool isReady);

    Task MatchStarted(MatchInit initialState);
    Task UpdateWorldState(MatchState state);
    Task MatchEnded(MatchResult result);
    
    Task ReceiveChatMessage(string message);
    Task LevelUp(LevelListPosition[] levelListPositions);
}

public record LobbyMemberDto(
    int PlayerId,
    string Nickname,
    bool IsReady,
    string? ChosenFrog);