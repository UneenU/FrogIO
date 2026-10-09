namespace Server;

public interface ILobbyService
{
    bool AddUserToLobby(string lobbyId, string userId);
    void RemoveUserFromLobby(string lobbyId, string userId);
    MatchResult CloseLobby(string lobbyId, string userId);
}