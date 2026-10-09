namespace Server;

public class LobbyService : ILobbyService
{
    private readonly ILobbyRepository _lobbyRepository;

    public LobbyService(ILobbyRepository lobbyRepository)
    {
        _lobbyRepository = lobbyRepository;
    }

    public bool AddUserToLobby(string lobbyId, string userId)
    {
        var lobby = _lobbyRepository.GetLobbyById(lobbyId);
        var userLobby = _lobbyRepository.FindUserLobby(userId);
        
        if (userLobby is not null ||
            lobby is null ||
            lobby.Members.Any(p => p.UserId.ToString() == userId) ||
            lobby.IsFull) return false;
        
        _lobbyRepository.SetUserLobby(lobbyId, userId);
        return true;
    }

    public void RemoveUserFromLobby(string lobbyId, string userId)
    {
        // лобби или пользователя может не существовать
        _lobbyRepository.RemoveUserFromLobby(lobbyId, userId);
    }

    public MatchResult CloseLobby(string lobbyId, string userId)
    {
        throw new NotImplementedException();
    }
}