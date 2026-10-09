namespace Server.Match;

public interface IMatchRepository
{
    string? FindUserMatch(string userId);
}