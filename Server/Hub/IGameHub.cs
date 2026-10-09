namespace Server;

public interface IGameHub
{
    Task UpdateWorldState(MatchState state);
    Task PlayerJoinedTheLobby(LobbyMember player);
    Task PlayerLeftTheLobby(LobbyMember player);
    Task MatchStarted();
    Task MatchEnded(MatchResult result);
    Task LobbyWasClosed();
    Task ReceiveChatMessage(string gamerId, string message);
    Task PlayerReturnedInTheLobby(LobbyMember player);
}