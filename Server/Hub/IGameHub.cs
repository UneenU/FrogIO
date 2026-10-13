using Core;
using Server.Match;

namespace Server;

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