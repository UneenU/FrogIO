using Core.Events;
using Core.Match;

namespace Server.Match.MatchService;

public interface IMatchService
{
    (MatchInit matchInit, string matchId) StartMatch(Core.Match.MatchCreator.PlayerInfo[] playersInfo);
    void CancelMatch(string matchId);
    Task HandleUserInputAsync(int userId, Input input);
    Task HandleMatchStateAsync(Core.Match.Match match, MatchState state, ClientEvent[] clientEvents);
    Task OnMatchEndingAsync(Core.Match.Match match);
    Task OnMatchCanceledAsync(Core.Match.Match match);
    Task OnMatchFallAsync(Core.Match.Match match, Exception ex);
}