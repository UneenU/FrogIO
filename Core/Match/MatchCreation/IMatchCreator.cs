namespace Core.Match.MatchCreator;

public interface IMatchCreator
{
    public Match CreateMatch(PlayerInfo[] playersInfo);
}