namespace Server.Match;

public interface IMatchRepository
{
    string? FindUserMatch(int userId);
}

public class MatchRepository : IMatchRepository
{
    public string? FindUserMatch(int userId)
    {
        throw new NotImplementedException();
    }
}