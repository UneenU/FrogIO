namespace Server.Lobby.LobbyService;

public interface ILobbyService
{
    Task<string> CreateLobby(string lobbyName, int maxPlayers);
    Task<bool> AddUserToLobby(string lobbyId, int userId);
    Task RemoveUserFromLobby(string lobbyId, int userId);
    Task<bool> CloseLobbyAsync(string lobbyId);
    Task OnMembersReadyAsync(string lobbyId, int userId, bool ready, string heroId);
}