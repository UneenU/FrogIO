namespace Server.Lobby;

public class Lobby
{
    public string LobbyId { get; }

    private readonly List<LobbyMember> _members = new();
    
    // todo: сделать readonly
    public List<LobbyMember> Members => _members;
    public string LobbyName { get; }
    public int MaxPlayers { get; }

    public bool IsFull => Members.Count == MaxPlayers;
    public bool EveryoneIsReady => Members.All(member => member.IsReady);

    private Lobby(string lobbyName, string lobbyId, int maxPlayers)
    {
        LobbyId = lobbyId;
        LobbyName = lobbyName;
        MaxPlayers = maxPlayers;
    }

    public static Lobby NewLobby(string name, int maxPlayers)
    {
        if (maxPlayers <= 0) throw new ArgumentException(nameof(maxPlayers));
        var lobbyId = Guid.NewGuid().ToString();
        return new Lobby(name, lobbyId, maxPlayers);
    }

    public static Lobby Rehydrate(string lobbyId, string lobbyName, int maxPlayers, IEnumerable<LobbyMember> members)
    {
        var lobby = new Lobby(lobbyName, lobbyId, maxPlayers);
        foreach (var member in members)
        {
            lobby._members.Add(member);
        }

        return lobby;
    }
}