namespace Server;

public class LobbyMember(int userId, string nickname)
{
    public int UserId { get; } = userId;
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