namespace Server.Match.MatchService;

public interface IMatchService
{
    (MatchInit matchInit, string matchId) StartMatch(Core.Match.MatchCreator.PlayerInfo[] playersInfo);
    void StopMatch(string matchId);
    void HandleUserInput(int userId, Input input);
}