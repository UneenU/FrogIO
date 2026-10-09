namespace Server;

public interface ILobbyRepository
{
    Lobby? GetLobbyById(string lobbyId);
    bool SetUserLobby(string lobbyId, string userId);
    void RemoveUserFromLobby(string lobbyId, string userId);
    string? FindUserLobby(string userId);
    IReadOnlyList<LobbyMember> GetLobbyMembers(string lobbyId);
    void AddLobby(Lobby lobby);

}