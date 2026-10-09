namespace Server;

public class LobbyRepository : ILobbyRepository
{
    public Lobby? GetLobbyById(string lobbyId)
    {
        throw new NotImplementedException();
    }

    public bool SetUserLobby(string lobbyId, string userId)
    {
        throw new NotImplementedException();
    }

    public void RemoveUserFromLobby(string lobbyId, string userId)
    {
        throw new NotImplementedException();
    }

    public string? FindUserLobby(string userId)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<LobbyMember> GetLobbyMembers(string lobbyId)
    {
        throw new NotImplementedException();
    }

    public void AddLobby(Lobby lobby)
    {
        throw new NotImplementedException();
    }
}