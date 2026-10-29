namespace Server.Match;

public record MatchInit(
    float WorldWidth, float WorldHeight,
    PlayerInfo[] Players);

public record PlayerInfo(int UserId, int FrogId);
