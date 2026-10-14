namespace Server.Match.MatchService;

public interface IMatchService
{
    (MatchInit matchInit, string matchId) StartMatch(Core.Match.MatchCreator.PlayerInfo[] playersInfo);
    void StopMatch(string matchId);
    Task HandleUserInputAsync(int userId, Input input);
}