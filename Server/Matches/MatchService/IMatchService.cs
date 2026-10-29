using Core.Events;
using Core.Match;
using Core.Match.MatchCreator;
using Server.Data;
using Server.Matches;

namespace Server.Match.MatchService;

public interface IMatchService
{
    Task<(MatchInit matchInit, string matchId)> StartMatchAsync(PlayerInit[] playersInfo);
    Task<MatchInit> OnRejoinMatchAsync(int userId);
    void EndMatchRuntime(string matchId);
    Task HandleUserInputAsync(int userId, Input input);
    Task HandleMatchStateAsync(Core.Match.Match match, MatchState state, ClientEvent[] clientEvents);
    Task OnMatchEndingAsync(Core.Match.Match match, MatchStatus status);
    Task OnMatchCanceledAsync(Core.Match.Match match);
    Task OnMatchFallAsync(Core.Match.Match match, Exception ex);
}