namespace Server;

public interface ILobbyService
{
    Task<string> CreateLobby(string lobbyName, int maxPlayers);
    Task<bool> AddUserToLobby(string lobbyId, string userId);
    Task RemoveUserFromLobby(string lobbyId, string userId);
    Task<bool> CloseLobbyAsync(string lobbyId);
    Task OnMembersReadyAsync(string lobbyId, string userId, bool ready, string heroId);
}