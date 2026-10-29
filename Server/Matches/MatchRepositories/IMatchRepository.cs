namespace Server.Match.MatchRepository;

public interface IMatchRepository
{
    ValueTask<string?> FindUserMatchAsync(int userId);
    ValueTask SetUserMatchAsync(int userId, string matchId);
    Task RemoveMatchAsync(string matchId);
}