using Core;

namespace Server;

public class Lobby
{
    public string LobbyId { get; }
    public List<LobbyMember> Members { get; } = new();
    // public bool IsStarted { get; set; } = false;
    public int MaxPlayers { get; }
    public bool IsFull => Members.Count == MaxPlayers;
    public int? MatchId { get; set; }
    
    private Lobby(string lobbyId, int maxPlayers)
    {
        LobbyId = lobbyId;
        MaxPlayers = maxPlayers;
    }

    public static Lobby NewLobby(int maxPlayers)
    {
        if (maxPlayers <= 0) throw new ArgumentException(nameof(maxPlayers));
        var lobbyId = Guid.NewGuid().ToString();
        return new Lobby(lobbyId, maxPlayers);
    }

}