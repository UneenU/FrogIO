using Server.Match;
using PlayerInfo = Core.Match.PlayerInfo;

namespace Server.MatchRuntime;

public interface IMatchService
{
    (MatchInit matchInit, string matchId) StartMatch(PlayerInfo[] playersInfo);
    void StopMatch(string matchId);
    void HandleUserInput(int userId, Input input);
}