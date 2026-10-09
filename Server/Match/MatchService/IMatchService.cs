using Core;
using Core.Commands;

namespace Server.MatchRuntime;

public interface IMatchService
{
    string StartMatch(string[] playersIds);
    void StopMatch(string matchId);
    void HandleUserInput(string userId, Input input);
}