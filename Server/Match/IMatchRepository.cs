namespace Server.Match;

public interface IMatchRepository
{
    string? FindUserMatch(string userId);
}

public class MatchRepository : IMatchRepository
{
    public string? FindUserMatch(string userId)
    {
        throw new NotImplementedException();
    }
}