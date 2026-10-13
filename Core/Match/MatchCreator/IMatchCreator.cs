using Core;

namespace Core.Match;

public interface IMatchCreator
{
    public Match CreateMatch(PlayerInfo[] playersInfo);
}