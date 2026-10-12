namespace Server;

public class LobbyMember(string userId, string nickname)
{
    public string UserId { get; } = userId;
    public string Nickname { get; } = nickname;
    public bool IsReady { get; private set; }
    public string? ChosenHero { get; private set; }
    
    public void SetChosenHero(string heroName)
    {
        ChosenHero = heroName;
    }

    public void SetReady(bool ready)
    {
        IsReady = ready;
    }
}