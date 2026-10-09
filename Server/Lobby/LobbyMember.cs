namespace Server;

public class LobbyMember(string userId, string nickname)
{
    public string UserId { get; } = userId;
    public string Nickname { get; } = nickname;
}