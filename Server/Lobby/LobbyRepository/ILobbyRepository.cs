namespace Server;

public interface ILobbyRepository
{
    Task<Lobby?> GetLobbyByIdAsync(string lobbyId);
    Task<bool> SetLobbyMemberAsync(string lobbyId, LobbyMember member);
    Task RemoveUserFromLobbyAsync(string lobbyId, int userId);
    // сделать возвращаемым значением Lobby
    Task<string?> FindUserLobbyAsync(int userId);
    Task<IReadOnlyList<LobbyMember>> GetLobbyMembersAsync(string lobbyId);
    Task AddLobbyAsync(Lobby lobby);
    Task RemoveLobbyAsync(string lobbyId);

    Task<List<Lobby>> GetLobbiesByNameAsync(string lobbyName);
    Task<LobbyMember> GetLobbyMemberAsync(string lobbyId, int userId);
}