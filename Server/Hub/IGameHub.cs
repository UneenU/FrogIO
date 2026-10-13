using Core.Match;
using Server.Lobby;
using Server.Match;

namespace Server.Hub;

public interface IGameHub
{
    Task UpdateWorldState(MatchState state);
    Task PlayerJoinedTheLobby(LobbyMember player);
    Task PlayerLeftTheLobby(LobbyMember player);
    Task MatchStarted(MatchInit initialState);
    Task MatchEnded(MatchResult result);
    Task LobbyWasClosed();
    Task ReceiveChatMessage(string message);
    Task PlayerReturnedInTheLobby(LobbyMember player);
    Task PlayerReadyChanged(int playerId, bool isReady);
    
    Task TimeToStart(TimeSpan timeToStart);
}